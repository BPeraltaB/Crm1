using System.Globalization;
using Crm1.WindowsForms.Models;
using Crm1.WindowsForms.Services;
using Task = Crm1.WindowsForms.Models.Task;
using TaskStatus = Crm1.WindowsForms.Models.TaskStatus;

namespace Crm1.WindowsForms.Forms;

/// <summary>
/// Formulario del módulo de tareas (specs/005-tareas.md).
/// La tabla es de solo lectura por defecto: Nuevo activa una fila para
/// editarla y Guardar la persiste (skill «modificar»); Modificar y Eliminar
/// están pendientes. La lógica de negocio vive en TaskService (constitución,
/// punto 3).
/// </summary>
public partial class TaskForm : System.Windows.Forms.Form
{
    // Color del borde que marca las celdas con errores de validación.
    private static readonly System.Drawing.Color ErrorBorderColor = System.Drawing.Color.FromArgb(220, 38, 38);

    // Color y grosor del borde que marca la fila seleccionada, igual que en
    // los demás módulos (skill «seleccionar-cliente»).
    private static readonly System.Drawing.Color SelectionBorderColor = System.Drawing.Color.FromArgb(100, 116, 139);
    private const float SelectionBorderWidth = 3F;

    // Color del texto de las filas deshabilitadas mientras hay edición abierta.
    private static readonly System.Drawing.Color DisabledRowTextColor = System.Drawing.Color.FromArgb(156, 163, 175);

    // Textos por defecto con los que nace cada fila nueva (specs/005 §4).
    private const string DefaultStatusText = "Pendiente";
    private const string DefaultPriorityText = "Media";

    // Almacén SQL con las tareas (LoadTasks/SaveTasks, specs/005 y specs/006).
    private readonly SqlDataStore? _dataStore;

    // Reglas de negocio de las tareas; null si la base no se pudo leer.
    private TaskService? _taskService;

    // Falso si la base de datos no se pudo abrir o leer: todos los botones
    // de gestión quedan inactivos (constitución, punto 5).
    private bool _canSave;

    // Fila activa para edición (rastreada por referencia); null si no hay sesión.
    private System.Windows.Forms.DataGridViewRow? _activeRow;

    // Instantánea de los valores de la fila activa al empezar la edición,
    // para restaurarlos si se pulsa Cancelar.
    private List<object?> _activeRowSnapshot = new();

    // Última copia guardada de las tareas, para recalcular si quedan cambios.
    private List<Task> _savedTasks = new();

    // Celdas con errores de validación: reciben un borde rojo alrededor.
    private readonly HashSet<System.Windows.Forms.DataGridViewCell> _errorCells = new();

    // Verdadero mientras la tabla se rellena desde disco (no son cambios del usuario).
    private bool _loading;

    // Guarda anti-reentrada al devolver la selección a la fila en edición.
    private bool _restoringSelection;

    // Verdadero cuando hay cambios sin guardar.
    private bool _dirty;

    // Ruta y sello temporal de la base de datos: detecta tareas escritas desde
    // fuera para recargar al volver a mostrarse (mismo criterio que ClientForm).
    private readonly string _databaseFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "crm1.db");
    private DateTime _lastDatabaseStamp;

    // Mapas del desplegable «Cliente/Futuro cliente»: texto visible ↔ registro.
    private readonly Dictionary<string, (Guid Id, TaskRelatedType Type)> _relatedByText = new();
    private readonly Dictionary<Guid, string> _relatedTextById = new();

    // Tarea actual marcada por el usuario (skill «seleccionar-cliente»):
    // rastreo por identificador, nunca por índice.
    private Guid? _currentTaskId;

    // Filtro activo por registro vinculado (botón Tareas de ClientForm o
    // ContactForm, specs/005 §7); null = sin filtro, se ven todas las tareas.
    private TaskRelatedType? _filterType;
    private Guid? _filterId;
    private string _filterText = string.Empty;

    // Tareas ocultas por el filtro: se conservan en memoria y se guardan junto
    // con las visibles, así el filtro nunca hace perder tareas de otros.
    private List<Task> _hiddenTasks = new();

    /// <summary>
    /// Identificador de la tarea actual (fila marcada con un clic en cualquier
    /// celda), o null si no hay selección. Skill «seleccionar-cliente».
    /// </summary>
    public Guid? CurrentTaskId => _currentTaskId;

    /// <summary>
    /// Tarea actual reconstruida de la fila marcada, o null si no hay marca.
    /// </summary>
    public Task? CurrentTask => FindRowById(_currentTaskId) is { } row
        ? MapRowToTask(row, out _)
        : null;

    /// <summary>
    /// Verdadero cuando la tabla tiene cambios sin guardar. Lo consulta MainForm
    /// antes de cerrar la aplicación porque TaskForm va embebido (specs/005 §7).
    /// </summary>
    public bool HasUnsavedChanges => _dirty;

    /// <summary>
    /// Muestra solo las tareas del cliente o futuro cliente indicado, con el
    /// aviso «Tareas de: …» y el botón «Ver todas» (botón Tareas de otros
    /// formularios, specs/005 §7). Con edición o cambios pendientes no se
    /// cambia de vista: primero se guarda o se cancela (constitución, punto 5).
    /// Guardar conserva las tareas ocultas por el filtro.
    /// </summary>
    public void ShowTasksOf(TaskRelatedType relatedType, Guid relatedId, string displayName)
    {
        if (_activeRow is not null || _dirty)
        {
            return;
        }

        _filterType = relatedType;
        _filterId = relatedId;
        _filterText = $"{(relatedType == TaskRelatedType.Client ? "(Cliente)" : "(Futuro cliente)")} {displayName}";
        filterLabel.Text = $"Tareas de: {_filterText}";
        filterLabel.Visible = true;
        showAllButton.Visible = true;
        LoadTasks();
    }

    /// <summary>
    /// Quita el filtro y vuelve a mostrar todas las tareas (botón «Ver todas»
    /// o item «Tareas» del menú).
    /// </summary>
    public void ShowAllTasks()
    {
        if (_activeRow is not null || _dirty)
        {
            return;
        }

        _filterType = null;
        _filterId = null;
        _filterText = string.Empty;
        filterLabel.Visible = false;
        showAllButton.Visible = false;
        LoadTasks();
    }

    /// <summary>
    /// Atiende el botón «Ver todas»: quita el filtro del listado.
    /// </summary>
    private void ShowAllButton_Click(object? sender, EventArgs e) => ShowAllTasks();

    /// <summary>
    /// Diálogos del formulario (avisos, confirmaciones y errores): todo pasa
    /// por aquí. En la aplicación es un MessageBox con este formulario como
    /// dueño; las pruebas automatizadas de interfaz lo sustituyen por un
    /// delegado que responde sin bloquear.
    /// </summary>
    internal Func<string, string, MessageBoxButtons, MessageBoxIcon, DialogResult> Prompt { get; set; }

    /// <summary>
    /// Crea el formulario y carga las tareas guardadas. Si la base de datos
    /// falla, la tabla queda vacía y los botones inactivos (constitución,
    /// punto 5).
    /// </summary>
    public TaskForm() : this(null)
    {
    }

    /// <summary>
    /// Constructor con costura de pruebas: permite sustituir los diálogos
    /// antes de que la constructora cargue los datos, porque un error de
    /// base de datos durante la carga ya muestra un mensaje.
    /// </summary>
    internal TaskForm(Func<string, string, MessageBoxButtons, MessageBoxIcon, DialogResult>? prompt)
    {
        InitializeComponent();

        // En la aplicación los diálogos son MessageBox con este formulario
        // como dueño (costura de pruebas: ver el comentario de la propiedad).
        Prompt = prompt ?? ((text, caption, buttons, icon) =>
            MessageBox.Show(this, text, caption, buttons, icon));

        try
        {
            _dataStore = new SqlDataStore();
        }
        catch (Exception ex)
        {
            ReportDataError(ex);
            return;
        }

        LoadTasks();
    }

    /// <summary>
    /// Carga en la tabla las tareas guardadas y prepara el desplegable de
    /// «Cliente/Futuro cliente» con la unión de clientes y futuros clientes.
    /// </summary>
    private void LoadTasks()
    {
        List<Task> tasks;
        List<Client> clients;
        List<Contact> contacts;
        try
        {
            tasks = _dataStore!.LoadTasks();
            clients = _dataStore.LoadClients();
            contacts = _dataStore.LoadContacts();
        }
        catch (Exception ex)
        {
            ReportDataError(ex);
            return;
        }

        _loading = true;
        tasksDataGridView.Rows.Clear();
        PopulateRelatedColumn(clients, contacts);

        // Con el filtro activo solo se muestran las tareas del registro
        // vinculado; las demás quedan ocultas y se conservan para Guardar.
        List<Task> visible = tasks;
        if (_filterId is Guid filterId && _filterType is TaskRelatedType filterType)
        {
            visible = tasks.Where(task => task.RelatedId == filterId && task.RelatedType == filterType).ToList();
            _hiddenTasks = tasks.Where(task => task.RelatedId != filterId || task.RelatedType != filterType).ToList();
        }
        else
        {
            _hiddenTasks.Clear();
        }

        foreach (Task task in visible)
        {
            AddTaskRow(task);
        }

        _loading = false;
        _savedTasks = visible;
        _dirty = false;
        _canSave = true;
        _lastDatabaseStamp = GetDatabaseStamp();

        // Sin tarea actual hasta que el usuario marque una fila con un clic
        // (skill «seleccionar-cliente»).
        _currentTaskId = null;
        tasksDataGridView.ClearSelection();
        UpdateCounter();
        UpdateButtonStates();
    }

    /// <summary>
    /// Rellena el desplegable «Cliente/Futuro cliente» con la unión de clientes
    /// y futuros clientes, y construye los mapas texto ↔ registro que usa
    /// Guardar para reconstruir el vínculo. Si dos registros se llaman igual,
    /// el segundo añade « (2)» para que el texto sea único.
    /// </summary>
    private void PopulateRelatedColumn(List<Client> clients, List<Contact> contacts)
    {
        relatedColumn.Items.Clear();
        _relatedByText.Clear();
        _relatedTextById.Clear();

        foreach (Client client in clients)
        {
            AddRelatedItem(
                $"(Cliente) {DisplayName(client.FirstName, client.LastName1, client.LastName2)}",
                client.Id, TaskRelatedType.Client);
        }
        foreach (Contact contact in contacts)
        {
            AddRelatedItem(
                $"(Futuro cliente) {DisplayName(contact.FirstName, contact.LastName1, contact.LastName2)}",
                contact.Id, TaskRelatedType.Prospect);
        }

        // Las reglas de negocio necesitan saber qué registros existen.
        var relatedRecords = _relatedByText.Values.ToDictionary(item => item.Id, item => item.Type);
        _taskService = new TaskService(relatedRecords);
    }

    // Añade una entrada al desplegable con un texto único.
    private void AddRelatedItem(string baseText, Guid id, TaskRelatedType type)
    {
        string text = baseText;
        for (int attempt = 2; _relatedByText.ContainsKey(text); attempt++)
        {
            text = $"{baseText} ({attempt})";
        }

        _relatedByText[text] = (id, type);
        _relatedTextById[id] = text;
        relatedColumn.Items.Add(text);
    }

    // Nombre para mostrar: nombre y apellidos no vacíos (razón social en
    // empresas, apellidos ausentes en muchos futuros clientes).
    private static string DisplayName(string firstName, string lastName1, string lastName2) =>
        string.Join(" ", new[] { firstName, lastName1, lastName2 }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

    /// <summary>
    /// Añade a la tabla una fila con los datos de una tarea guardada.
    /// </summary>
    private void AddTaskRow(Task task)
    {
        // Si el registro vinculado ya no existe, se muestra «—»; se añade a la
        // lista del desplegable para que la celda pueda pintarlo (mismo truco
        // que ClientForm con las provincias no oficiales).
        string related = _relatedTextById.TryGetValue(task.RelatedId, out string? text)
            ? text
            : "—";
        if (related == "—" && !relatedColumn.Items.Contains("—"))
        {
            relatedColumn.Items.Add("—");
        }

        int index = tasksDataGridView.Rows.Add(
            task.Action,
            task.Description,
            related,
            task.DueDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            StatusText(task.Status),
            PriorityText(task.Priority));

        // El identificador de la tarea viaja en la fila sin mostrarse.
        tasksDataGridView.Rows[index].Tag = task.Id;
    }

    // Etiquetas en español de estado y prioridad (constitución, punto 6);
    // coinciden con los elementos de los desplegables del Designer.
    private static string StatusText(TaskStatus status) => status switch
    {
        TaskStatus.InProgress => "En curso",
        TaskStatus.Completed => "Completada",
        _ => "Pendiente"
    };

    private static string PriorityText(TaskPriority priority) => priority switch
    {
        TaskPriority.Low => "Baja",
        TaskPriority.High => "Alta",
        _ => "Media"
    };

    /// <summary>
    /// Actualiza los botones según el estado (normal o edición) y si se
    /// pueden validar y guardar datos (skill «modificar»): en edición, Nuevo,
    /// Modificar y Eliminar inactivos; Cancelar activo.
    /// </summary>
    private void UpdateButtonStates()
    {
        bool editing = _activeRow is not null;

        newButton.Enabled = _canSave && !editing;
        modifyButton.Enabled = _canSave && !editing;
        deleteButton.Enabled = _canSave && !editing;
        saveButton.Enabled = _canSave;
        cancelButton.Enabled = _canSave && editing;
    }

    /// <summary>
    /// Inserta una fila vacía como primera fila con los valores por defecto
    /// (Pendiente y Media), la activa para edición y despliega la Acción.
    /// </summary>
    private void NewButton_Click(object? sender, EventArgs e)
    {
        if (!_canSave || _activeRow is not null)
        {
            return;
        }

        tasksDataGridView.Rows.Insert(0, 1);
        System.Windows.Forms.DataGridViewRow row = tasksDataGridView.Rows[0];
        row.Cells["statusColumn"].Value = DefaultStatusText;
        row.Cells["priorityColumn"].Value = DefaultPriorityText;

        // Con el filtro activo, la tarea nueva nace vinculada al registro
        // filtrado (decisión del usuario: todos los botones, pero solo para él).
        if (_filterId is not null)
        {
            row.Cells["relatedColumn"].Value = _filterText;
        }

        ActivateRowEditing(row);

        // Selecciona la fila nueva y entra a editar la celda Acción.
        tasksDataGridView.CurrentCell = row.Cells["actionColumn"];
        tasksDataGridView.BeginEdit(true);
    }

    /// <summary>
    /// Activa una fila para edición (solo ella pierde la solo lectura) y toma
    /// una instantánea de sus valores para poder cancelar.
    /// </summary>
    private void ActivateRowEditing(System.Windows.Forms.DataGridViewRow row)
    {
        // Instantánea de los valores actuales para Cancelar.
        _activeRowSnapshot = row.Cells.Cast<System.Windows.Forms.DataGridViewCell>()
            .Select(cell => (object?)cell.Value)
            .ToList();

        _activeRow = row;
        row.ReadOnly = false;

        // Con el filtro activo, el vínculo queda fijo en la fila editada: las
        // tareas creadas o modificadas siguen siendo del registro filtrado.
        // Ojo con WinForms: poner una celda ReadOnly=false DESPUÉS de poner
        // su fila ReadOnly=true reactiva la fila, así que solo se toca aquí.
        row.Cells["relatedColumn"].ReadOnly = _filterId is not null;

        // Durante la edición, la tarea actual es la de esta fila (skill
        // «seleccionar-cliente», regla 6).
        if (row.Tag is Guid activeId)
        {
            _currentTaskId = activeId;
        }

        // Las demás filas quedan deshabilitadas: atenuadas en gris y sin
        // selección posible mientras dure la edición (skill «modificar»).
        foreach (System.Windows.Forms.DataGridViewRow other in tasksDataGridView.Rows)
        {
            if (other != row)
            {
                other.DefaultCellStyle.ForeColor = DisabledRowTextColor;
            }
        }

        // Bloquea la ordenación por cabeceras mientras hay una fila abierta.
        SetColumnsSortable(false);

        UpdateButtonStates();
    }

    /// <summary>
    /// Cierra la sesión de edición dejando todas las filas en solo lectura.
    /// </summary>
    private void EndEditingSession()
    {
        if (_activeRow is null)
        {
            return;
        }

        _activeRow.ReadOnly = true;
        _activeRow = null;
        _activeRowSnapshot.Clear();

        // Rehabilita todas las filas: color normal y selección posible.
        foreach (System.Windows.Forms.DataGridViewRow gridRow in tasksDataGridView.Rows)
        {
            gridRow.DefaultCellStyle.ForeColor = System.Drawing.SystemColors.ControlText;
        }
        SetColumnsSortable(true);
        UpdateButtonStates();
        UpdateCounter();
    }

    /// <summary>
    /// Activa o desactiva la ordenación por cabeceras (bloqueada durante la
    /// edición para no descuadrar la fila abierta). Las columnas de
    /// desplegable nunca ordenan.
    /// </summary>
    private void SetColumnsSortable(bool sortable)
    {
        foreach (System.Windows.Forms.DataGridViewColumn column in tasksDataGridView.Columns)
        {
            if (column is System.Windows.Forms.DataGridViewComboBoxColumn)
            {
                continue;
            }

            column.SortMode = sortable
                ? System.Windows.Forms.DataGridViewColumnSortMode.Automatic
                : System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
        }
    }

    /// <summary>
    /// Activa las celdas de la tarea marcada para reescribir las que se
    /// quieran (skill «modificar»).
    /// </summary>
    private void ModifyButton_Click(object? sender, EventArgs e)
    {
        if (!_canSave || _activeRow is not null)
        {
            return;
        }

        // Tarea marcada con un clic en cualquier celda de su fila (skill
        // «seleccionar-cliente»): la fila se localiza por identificador.
        System.Windows.Forms.DataGridViewRow? row = FindRowById(_currentTaskId);
        if (row is null)
        {
            Prompt(
                "Selecciona una tarea de la tabla.",
                "CRM1 — Modificar",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ActivateRowEditing(row);

        // Coloca el cursor en la primera celda de la fila.
        tasksDataGridView.CurrentCell = row.Cells["actionColumn"];
        tasksDataGridView.BeginEdit(true);
    }

    /// <summary>
    /// Elimina la tarea marcada tras pedir confirmación con su nombre (acción
    /// y registro vinculado). La eliminación es en memoria: solo se hace
    /// permanente al pulsar Guardar (skill «borrar»; constitución, punto 5).
    /// </summary>
    private void DeleteButton_Click(object? sender, EventArgs e)
    {
        if (!_canSave || _activeRow is not null)
        {
            return;
        }

        // Tarea marcada con un clic (skill «seleccionar-cliente»).
        System.Windows.Forms.DataGridViewRow? row = FindRowById(_currentTaskId);
        if (row is null)
        {
            Prompt(
                "Selecciona una tarea de la tabla.",
                "CRM1 — Eliminar",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Confirmación antes de eliminar, identificando la tarea por su
        // acción y su registro vinculado (decisión del usuario).
        DialogResult result = Prompt(
            $"¿Seguro que quieres eliminar la tarea {RowDisplayName(row)}?\n\n" +
            "Se quitará de la tabla y se eliminará por completo de la base de datos al pulsar Guardar.",
            "CRM1 — Eliminar",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        // Eliminación en memoria: solo Guardar escribe en disco.
        tasksDataGridView.Rows.Remove(row);

        // Si la fila eliminada era la marcada, la marca se limpia (regla 5).
        EnsureCurrentTaskExists();

        // Queda un cambio sin guardar pendiente de Guardar.
        _dirty = true;

        // El contador debe reflejar la fila retirada también sin edición
        // abierta (EndEditingSession vuelve pronto si no hay sesión).
        UpdateCounter();
    }

    /// <summary>
    /// Nombre visible de una fila de tarea: su acción y el registro vinculado
    /// («Visitar de (Cliente) Ana»), para identificarla sin ambigüedad.
    /// </summary>
    private static string RowDisplayName(System.Windows.Forms.DataGridViewRow row)
    {
        string action = CellText(row, "actionColumn");
        string related = CellText(row, "relatedColumn");

        if (action.Length > 0 && related.Length > 0)
        {
            return $"{action} de {related}";
        }

        return action.Length > 0 ? action : "esta tarea";
    }

    /// <summary>
    /// Descarta la edición en curso: una fila nueva se elimina de la tabla y
    /// una modificada vuelve a los valores de la instantánea tomada al
    /// activarla.
    /// </summary>
    private void CancelButton_Click(object? sender, EventArgs e)
    {
        if (_activeRow is null)
        {
            return;
        }

        // Confirma la celda en edición antes de restaurar.
        tasksDataGridView.EndEdit();

        System.Windows.Forms.DataGridViewRow row = _activeRow;

        if (row.Tag is not Guid)
        {
            // Fila creada con Nuevo y aún no guardada: se elimina.
            tasksDataGridView.Rows.Remove(row);
        }
        else
        {
            // Fila guardada antes: vuelve a los valores anteriores.
            for (int i = 0; i < row.Cells.Count && i < _activeRowSnapshot.Count; i++)
            {
                row.Cells[i].Value = _activeRowSnapshot[i];
            }
        }

        // Si la fila retirada era la marcada, la marca se limpia (regla 5).
        EnsureCurrentTaskExists();

        EndEditingSession();
        ClearErrorMarks();

        // Recalcula si quedan otros cambios sin guardar (comparación con la
        // última copia guardada) para que el aviso al cerrar nunca mienta.
        _dirty = GridDiffersFromSaved();
    }

    /// <summary>
    /// Valida toda la tabla y, si todo es correcto, guarda las tareas en la
    /// base de datos (specs/005).
    /// </summary>
    private void SaveButton_Click(object? sender, EventArgs e)
    {
        if (!_canSave)
        {
            return;
        }

        // Confirma la celda en edición para leer su último valor.
        tasksDataGridView.EndEdit();

        // Limpia las marcas rojas de una validación anterior.
        ClearErrorMarks();

        var errors = new List<(int Row, TaskValidationError Error)>();
        var tasks = new List<Task>();
        var rowIndexes = new List<int>();

        for (int i = 0; i < tasksDataGridView.Rows.Count; i++)
        {
            System.Windows.Forms.DataGridViewRow row = tasksDataGridView.Rows[i];

            // Las filas totalmente vacías se ignoran (p. ej. un Nuevo por
            // accidente); los valores por defecto no cuentan como contenido.
            if (IsEmptyRow(row))
            {
                continue;
            }

            // Recorta los valores antes de validar y guardar.
            NormalizeRowCells(row);

            Task task = MapRowToTask(row, out TaskValidationError? error);
            if (error is not null)
            {
                errors.Add((i, error));
                continue;
            }

            tasks.Add(task);
            rowIndexes.Add(i);
        }

        // Reglas de negocio fuera del formulario (constitución, punto 3).
        foreach (TaskRowValidationError rowError in _taskService!.ValidateTasks(tasks))
        {
            errors.Add((rowIndexes[rowError.RowIndex], rowError.Error));
        }

        if (errors.Count > 0)
        {
            ShowValidationErrors(errors);
            return;
        }

        // Asigna identificadores a las filas que aún no tienen tarea guardada.
        for (int i = 0; i < tasks.Count; i++)
        {
            if (tasks[i].Id == Guid.Empty)
            {
                tasks[i].Id = Guid.NewGuid();
            }
            tasksDataGridView.Rows[rowIndexes[i]].Tag = tasks[i].Id;
        }

        try
        {
            // Con el filtro activo, las tareas ocultas se conservan: guardar
            // escribe las visibles más las de los demás registros.
            List<Task> tasksToSave = _hiddenTasks.Concat(tasks).ToList();
            _dataStore!.SaveTasks(tasksToSave);
        }
        catch (Exception ex)
        {
            // Un fallo de escritura nunca se presenta como éxito (constitución, punto 5).
            Prompt(
                "No se han podido guardar las tareas.\n\n" + ex.Message,
                "CRM1 — Error al guardar",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _savedTasks = tasks;
        _dirty = false;
        _lastDatabaseStamp = GetDatabaseStamp();

        // La tarea actual pasa a ser la de la fila editada, ya con su
        // identificador asignado (skill «seleccionar-cliente», regla 6).
        if (_activeRow is not null && _activeRow.Tag is Guid savedId)
        {
            _currentTaskId = savedId;
        }

        // Todo vuelve a solo lectura y las filas vacías se retiran de la vista.
        RemoveEmptyRows();
        EndEditingSession();
        EnsureCurrentTaskExists();

        // Sin sesión de edición (p. ej. solo se eliminó una fila),
        // EndEditingSession vuelve pronto: el contador se refresca aquí.
        UpdateCounter();

        Prompt(
            $"Se han guardado {tasks.Count} tarea(s) correctamente.",
            "CRM1 — Guardar",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>
    /// Retira de la vista las filas totalmente vacías (nunca se guardan).
    /// </summary>
    private void RemoveEmptyRows()
    {
        for (int i = tasksDataGridView.Rows.Count - 1; i >= 0; i--)
        {
            if (IsEmptyRow(tasksDataGridView.Rows[i]))
            {
                tasksDataGridView.Rows.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Indica si la tabla (filas no vacías) difiere de la última copia
    /// guardada, comparando por identificador para no depender del orden.
    /// </summary>
    private bool GridDiffersFromSaved()
    {
        var current = new List<Task>();
        for (int i = 0; i < tasksDataGridView.Rows.Count; i++)
        {
            System.Windows.Forms.DataGridViewRow row = tasksDataGridView.Rows[i];
            if (IsEmptyRow(row))
            {
                continue;
            }
            current.Add(MapRowToTask(row, out _));
        }

        if (current.Count != _savedTasks.Count)
        {
            return true;
        }

        var savedById = new Dictionary<Guid, Task>();
        foreach (Task task in _savedTasks)
        {
            savedById[task.Id] = task;
        }

        foreach (Task task in current)
        {
            if (!savedById.TryGetValue(task.Id, out Task? saved) || !TasksEqual(task, saved))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Compara dos tareas campo a campo.
    /// </summary>
    private static bool TasksEqual(Task a, Task b)
    {
        return string.Equals(a.Action, b.Action, StringComparison.Ordinal)
            && string.Equals(a.Description, b.Description, StringComparison.Ordinal)
            && a.DueDate == b.DueDate
            && a.Status == b.Status
            && a.Priority == b.Priority
            && a.RelatedId == b.RelatedId
            && a.RelatedType == b.RelatedType;
    }

    /// <summary>
    /// Convierte una fila en una tarea; los errores de forma (fecha, vínculo o
    /// desplegable) se devuelven como error de validación.
    /// </summary>
    private Task MapRowToTask(System.Windows.Forms.DataGridViewRow row, out TaskValidationError? error)
    {
        error = null;

        // Acción: desplegable; vacía se valida en el servicio.
        string action = CellText(row, "actionColumn");

        // Vínculo: el texto del desplegable se traduce a (Id, tipo).
        Guid relatedId = Guid.Empty;
        TaskRelatedType relatedType = TaskRelatedType.Client;
        string relatedText = CellText(row, "relatedColumn");
        if (relatedText.Length == 0 || relatedText == "—" || !_relatedByText.TryGetValue(relatedText, out (Guid Id, TaskRelatedType Type) related))
        {
            error = new TaskValidationError
            {
                Field = nameof(Task.RelatedId),
                Message = "Debes elegir un cliente o futuro cliente."
            };
        }
        else
        {
            relatedId = related.Id;
            relatedType = related.Type;
        }

        // Estado: desplegable con tres valores.
        string statusText = CellText(row, "statusColumn");
        TaskStatus status = statusText switch
        {
            "En curso" => TaskStatus.InProgress,
            "Completada" => TaskStatus.Completed,
            "Pendiente" => TaskStatus.Pending,
            _ => TaskStatus.Pending
        };
        if (error is null && statusText is not ("Pendiente" or "En curso" or "Completada"))
        {
            error = new TaskValidationError
            {
                Field = nameof(Task.Status),
                Message = "El estado no es válido."
            };
        }

        // Prioridad: desplegable con tres valores.
        string priorityText = CellText(row, "priorityColumn");
        TaskPriority priority = priorityText switch
        {
            "Baja" => TaskPriority.Low,
            "Alta" => TaskPriority.High,
            "Media" => TaskPriority.Medium,
            _ => TaskPriority.Medium
        };
        if (error is null && priorityText is not ("Baja" or "Media" or "Alta"))
        {
            error = new TaskValidationError
            {
                Field = nameof(Task.Priority),
                Message = "La prioridad no es válida."
            };
        }

        // Fecha límite en formato dd/MM/aaaa.
        DateTime dueDate = default;
        if (error is null)
        {
            string dateText = CellText(row, "dueDateColumn");
            if (dateText.Length == 0)
            {
                error = new TaskValidationError
                {
                    Field = nameof(Task.DueDate),
                    Message = "La celda está vacía."
                };
            }
            else if (!DateTime.TryParseExact(dateText, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dueDate))
            {
                error = new TaskValidationError
                {
                    Field = nameof(Task.DueDate),
                    Message = "La fecha no es válida (dd/mm/aaaa)."
                };
            }
        }

        return new Task
        {
            Id = row.Tag is Guid id ? id : Guid.Empty,
            Action = action,
            Description = CellText(row, "descriptionColumn"),
            DueDate = dueDate,
            Status = status,
            Priority = priority,
            RelatedId = relatedId,
            RelatedType = relatedType
        };
    }

    /// <summary>
    /// Recorta los valores de la fila antes de validar y guardar.
    /// </summary>
    private static void NormalizeRowCells(System.Windows.Forms.DataGridViewRow row)
    {
        foreach (System.Windows.Forms.DataGridViewCell cell in row.Cells)
        {
            if (cell.Value is string text)
            {
                cell.Value = text.Trim();
            }
        }
    }

    /// <summary>
    /// Indica si todas las celdas de la fila están vacías. Los valores por
    /// defecto de una fila nueva (Pendiente y Media) y el vínculo pre-llenado
    /// por el filtro no cuentan como contenido, para poder descartar Nuevos
    /// por accidente (mismo criterio que ContactForm con su estado Pendiente).
    /// </summary>
    private bool IsEmptyRow(System.Windows.Forms.DataGridViewRow row)
    {
        foreach (System.Windows.Forms.DataGridViewCell cell in row.Cells)
        {
            string value = cell.Value?.ToString()?.Trim() ?? string.Empty;

            // Los valores por defecto de Estado y Prioridad se ignoran.
            if (cell.OwningColumn.Name is "statusColumn" or "priorityColumn"
                && (value == "Pendiente" || value == "Media"))
            {
                continue;
            }

            // El vínculo pre-llenado por el filtro no cuenta como contenido.
            if (cell.OwningColumn.Name == "relatedColumn" && value == _filterText)
            {
                continue;
            }

            if (value.Length > 0)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Devuelve el texto de una celda de la fila (cadena vacía si es nula).
    /// </summary>
    private static string CellText(System.Windows.Forms.DataGridViewRow row, string columnName)
    {
        object? value = row.Cells[columnName].Value;
        return value?.ToString()?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Muestra una línea por error, solo con el mensaje corto, y marca las
    /// celdas afectadas con un borde rojo (specs/002 §4.1, mismo patrón).
    /// </summary>
    private void ShowValidationErrors(List<(int Row, TaskValidationError Error)> errors)
    {
        foreach (var (row, taskError) in errors)
        {
            string? columnName = ColumnNameFor(taskError.Field);
            if (columnName is not null)
            {
                _errorCells.Add(tasksDataGridView.Rows[row].Cells[columnName]);
            }
        }
        tasksDataGridView.Invalidate();

        IEnumerable<string> messages = errors.Select(item => item.Error.Message);
        Prompt(
            string.Join("\n", messages),
            "CRM1 — Errores de validación",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    /// <summary>
    /// Quita las marcas rojas de una validación anterior.
    /// </summary>
    private void ClearErrorMarks()
    {
        _errorCells.Clear();
        tasksDataGridView.Invalidate();
    }

    /// <summary>
    /// Nombre de columna de la tabla para un campo del modelo.
    /// </summary>
    private static string? ColumnNameFor(string field) => field switch
    {
        nameof(Task.Action) => "actionColumn",
        nameof(Task.Description) => "descriptionColumn",
        nameof(Task.RelatedId) => "relatedColumn",
        nameof(Task.DueDate) => "dueDateColumn",
        nameof(Task.Status) => "statusColumn",
        nameof(Task.Priority) => "priorityColumn",
        _ => null
    };

    /// <summary>
    /// Actualiza el contador del listado: sin filtro local — la búsqueda se
    /// hace desde el formulario principal (specs/004) — todas las filas están
    /// visibles (specs/005).
    /// </summary>
    private void UpdateCounter()
    {
        if (_loading)
        {
            return;
        }

        int totalCount = 0;
        foreach (System.Windows.Forms.DataGridViewRow row in tasksDataGridView.Rows)
        {
            row.Visible = true;
            if (!IsEmptyRow(row))
            {
                totalCount++;
            }
        }

        counterLabel.Text = $"Mostrando {totalCount} de {totalCount}";
    }

    /// <summary>
    /// Sello temporal de la base de datos: cambia cuando alguien más la escribe.
    /// </summary>
    private DateTime GetDatabaseStamp() =>
        File.Exists(_databaseFilePath) ? File.GetLastWriteTimeUtc(_databaseFilePath) : DateTime.MinValue;

    /// <summary>
    /// Al volver a mostrar el módulo, recarga las tareas si la base de datos
    /// cambió en disco y aquí no hay nada pendiente (mismo criterio que
    /// ClientForm).
    /// </summary>
    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);

        if (Visible && _canSave && !_dirty && _activeRow is null
            && GetDatabaseStamp() != _lastDatabaseStamp)
        {
            LoadTasks();
        }
    }

    /// <summary>
    /// Muestra el error de datos y desactiva la gestión (constitución, punto 5).
    /// </summary>
    private void ReportDataError(Exception ex)
    {
        Prompt(
            "No se pudo leer la base de datos de tareas (Data/crm1.db).\n\n" +
            "La tabla se muestra vacía y los botones quedan desactivados.\n\n" +
            ex.Message,
            "CRM1 — Error de datos",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
        _canSave = false;
        UpdateButtonStates();
    }

    /// <summary>
    /// Marca que hay cambios sin guardar cuando cambia una celda.
    /// </summary>
    private void TasksDataGridView_CellValueChanged(object? sender, System.Windows.Forms.DataGridViewCellEventArgs e)
    {
        if (!_loading && e.RowIndex >= 0)
        {
            _dirty = true;
        }

        // Al corregir una celda marcada, su borde rojo desaparece al momento.
        if (e.RowIndex >= 0 && e.ColumnIndex >= 0
            && _errorCells.Remove(tasksDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex]))
        {
            tasksDataGridView.Invalidate();
        }
    }

    /// <summary>
    /// Marca que hay cambios sin guardar cuando se añade una fila; toda fila
    /// nueva nace en solo lectura (la activa se desbloquea al activarse).
    /// </summary>
    private void TasksDataGridView_RowsAdded(object? sender, System.Windows.Forms.DataGridViewRowsAddedEventArgs e)
    {
        if (!_loading)
        {
            _dirty = true;
        }

        if (e.RowIndex >= 0)
        {
            tasksDataGridView.Rows[e.RowIndex].ReadOnly = true;
        }
    }

    /// <summary>
    /// Dibuja el borde rojo alrededor de las celdas con errores de validación.
    /// Las celdas permanecen siempre blancas para escribir.
    /// </summary>
    private void TasksDataGridView_CellPainting(object? sender, System.Windows.Forms.DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        System.Windows.Forms.DataGridViewCell cell = tasksDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex];
        if (!_errorCells.Contains(cell))
        {
            return;
        }

        e.Paint(e.CellBounds, System.Windows.Forms.DataGridViewPaintParts.All);
        if (e.Graphics is not null)
        {
            using var pen = new System.Drawing.Pen(ErrorBorderColor, 2F);
            e.Graphics.DrawRectangle(
                pen,
                e.CellBounds.X + 1, e.CellBounds.Y + 1,
                e.CellBounds.Width - 3, e.CellBounds.Height - 3);
        }
        e.Handled = true;
    }

    /// <summary>
    /// Evita que la tabla lance excepciones con valores ausentes de los
    /// desplegables.
    /// </summary>
    private void TasksDataGridView_DataError(object? sender, System.Windows.Forms.DataGridViewDataErrorEventArgs e)
    {
        e.ThrowException = false;
    }

    /// <summary>
    /// Durante la edición las demás filas están deshabilitadas: la selección
    /// (clic o teclado) se devuelve a la fila en edición (skill «modificar»).
    /// Si esa fila ya se retiró de la tabla, no se toca: sus celdas dejaron
    /// de pertenecer al control y reasignar la celda actual lanzaría «La
    /// celda proporcionada no pertenece a este control».
    /// </summary>
    private void TasksDataGridView_SelectionChanged(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        System.Windows.Forms.DataGridViewRow? selected = tasksDataGridView.SelectedRows.Count > 0
            ? tasksDataGridView.SelectedRows[0]
            : null;

        if (_activeRow is not null && _activeRow.Index >= 0
            && !_restoringSelection && selected != _activeRow)
        {
            _restoringSelection = true;
            try
            {
                tasksDataGridView.ClearSelection();

                // Devuelve también la celda actual a la fila en edición,
                // manteniendo la columna en la que estaba el usuario.
                int column = tasksDataGridView.CurrentCell?.ColumnIndex ?? 0;
                if (column >= 0 && column < _activeRow.Cells.Count)
                {
                    tasksDataGridView.CurrentCell = _activeRow.Cells[column];
                }

                _activeRow.Selected = true;
            }
            finally
            {
                _restoringSelection = false;
            }
            return;
        }

        if (_restoringSelection)
        {
            return;
        }

        // La fila seleccionada marca la tarea actual (clic o teclado); sin
        // selección no hay tarea actual, nunca un valor inventado (regla 1).
        _currentTaskId = selected?.Tag is Guid taskId ? taskId : null;
    }

    /// <summary>
    /// Dibuja el borde grueso alrededor de la fila seleccionada, igual que en
    /// los demás módulos. Las celdas siguen siempre blancas.
    /// </summary>
    private void TasksDataGridView_RowPostPaint(object? sender, System.Windows.Forms.DataGridViewRowPostPaintEventArgs e)
    {
        if (e.RowIndex < 0 || !tasksDataGridView.Rows[e.RowIndex].Selected)
        {
            return;
        }

        if (e.Graphics is not null)
        {
            using var pen = new System.Drawing.Pen(SelectionBorderColor, SelectionBorderWidth);
            e.Graphics.DrawRectangle(
                pen,
                e.RowBounds.X + 1, e.RowBounds.Y + 1,
                e.RowBounds.Width - 3, e.RowBounds.Height - 3);
        }
    }

    /// <summary>
    /// Busca la fila de la tarea con el identificador indicado (rastreo por
    /// Id, nunca por índice: admite ordenar o recargar la tabla).
    /// </summary>
    private System.Windows.Forms.DataGridViewRow? FindRowById(Guid? taskId)
    {
        if (taskId is null)
        {
            return null;
        }

        foreach (System.Windows.Forms.DataGridViewRow row in tasksDataGridView.Rows)
        {
            if (row.Tag is Guid id && id == taskId)
            {
                return row;
            }
        }

        return null;
    }

    /// <summary>
    /// Limpia la marca de tarea actual si su fila ya no existe (regla 5 de la
    /// skill «seleccionar-cliente»: sin identificadores huérfanos).
    /// </summary>
    private void EnsureCurrentTaskExists()
    {
        if (_currentTaskId is not null && FindRowById(_currentTaskId) is null)
        {
            _currentTaskId = null;
        }
    }
}
