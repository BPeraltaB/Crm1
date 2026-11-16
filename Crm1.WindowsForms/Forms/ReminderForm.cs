using System.Globalization;
using Crm1.WindowsForms.Models;
using Crm1.WindowsForms.Services;

namespace Crm1.WindowsForms.Forms;

/// <summary>
/// Formulario del módulo de recordatorios (specs/007). Carga desde la base
/// SQL y edición con Nuevo, Guardar y Cancelar (skill «modificar», primera
/// iteración): la tabla es de solo lectura por defecto y la fila activa se
/// desbloquea para editarla. Tras un guardado correcto la tabla se reordena
/// por fecha y hora (decisión del usuario). Modificar, Eliminar y los avisos
/// al abrir/cerrar la aplicación llegan en pasos siguientes. La lógica de
/// negocio vive en ReminderService (constitución, punto 3).
/// </summary>
public partial class ReminderForm : System.Windows.Forms.Form
{
    // Color del borde que marca las celdas con errores de validación.
    private static readonly System.Drawing.Color ErrorBorderColor = System.Drawing.Color.FromArgb(220, 38, 38);

    // Color y grosor del borde que marca la fila seleccionada, igual que en
    // los demás módulos (skill «seleccionar-cliente»).
    private static readonly System.Drawing.Color SelectionBorderColor = System.Drawing.Color.FromArgb(100, 116, 139);
    private const float SelectionBorderWidth = 3F;

    // Color del texto de las filas deshabilitadas mientras hay edición abierta.
    private static readonly System.Drawing.Color DisabledRowTextColor = System.Drawing.Color.FromArgb(156, 163, 175);

    // Texto por defecto con el que nace cada fila nueva (specs/007 §3).
    private const string DefaultStatusText = "Activo";

    // Almacén SQL con los recordatorios (LoadReminders/SaveReminders, specs/007
    // y specs/006).
    private readonly SqlDataStore? _dataStore;

    // Reglas de negocio de los recordatorios; null si la base no se pudo leer.
    private ReminderService? _reminderService;

    // Falso si la base de datos no se pudo abrir o leer: los botones de
    // gestión quedan inactivos (constitución, punto 5).
    private bool _canSave;

    // Fila activa para edición (rastreada por referencia); null si no hay sesión.
    private System.Windows.Forms.DataGridViewRow? _activeRow;

    // Instantánea de los valores de la fila activa al empezar la edición,
    // para restaurarlos si se pulsa Cancelar.
    private List<object?> _activeRowSnapshot = new();

    // Última copia guardada de los recordatorios, para recalcular si quedan cambios.
    private List<Reminder> _savedReminders = new();

    // Celdas con errores de validación: reciben un borde rojo alrededor.
    private readonly HashSet<System.Windows.Forms.DataGridViewCell> _errorCells = new();

    // Verdadero mientras la tabla se rellena desde disco (no son cambios del usuario).
    private bool _loading;

    // Guarda anti-reentrada al devolver la selección a la fila en edición.
    private bool _restoringSelection;

    // Verdadero cuando hay cambios sin guardar.
    private bool _dirty;

    // Ruta y sello temporal de la base de datos: detecta recordatorios escritos
    // desde fuera para recargar al volverse a mostrar (mismo criterio que
    // ClientForm y TaskForm).
    private readonly string _databaseFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "crm1.db");
    private DateTime _lastDatabaseStamp;

    // Mapas del desplegable «Cliente/Futuro cliente»: texto visible ↔ registro
    // (mismo mecanismo que TaskForm).
    private readonly Dictionary<string, (Guid Id, TaskRelatedType Type)> _relatedByText = new();
    private readonly Dictionary<Guid, string> _relatedTextById = new();

    // Recordatorio actual marcado por el usuario (skill «seleccionar-cliente»):
    // rastreo por identificador, nunca por índice.
    private Guid? _currentReminderId;

    /// <summary>
    /// Identificador del recordatorio actual (fila marcada con un clic en
    /// cualquier celda), o null si no hay selección. Skill «seleccionar-cliente».
    /// </summary>
    public Guid? CurrentReminderId => _currentReminderId;

    /// <summary>
    /// Recordatorio actual reconstruido de la fila marcada, o null si no hay
    /// marca.
    /// </summary>
    public Reminder? CurrentReminder => FindRowById(_currentReminderId) is { } row
        ? MapRowToReminder(row, out _)
        : null;

    /// <summary>
    /// Verdadero cuando la tabla tiene cambios sin guardar. Lo consulta MainForm
    /// antes de cerrar la aplicación porque ReminderForm va embebido (specs/007 §7).
    /// </summary>
    public bool HasUnsavedChanges => _dirty;

    /// <summary>
    /// Verdadero mientras hay una fila en edición (Nuevo o Modificar). Lo usa
    /// la búsqueda global antes de navegar hasta un recordatorio (specs/004 §7).
    /// </summary>
    public bool IsEditing => _activeRow is not null;

    /// <summary>
    /// Marca como recordatorio actual el del identificador indicado y desplaza
    /// la tabla hasta su fila (búsqueda global, specs/004 §7). Devuelve false
    /// si hay una edición en curso o el recordatorio no está en la tabla.
    /// </summary>
    public bool SelectReminderById(Guid reminderId)
    {
        if (_activeRow is not null)
        {
            return false;
        }

        System.Windows.Forms.DataGridViewRow? row = FindRowById(reminderId);
        if (row is null)
        {
            return false;
        }

        remindersDataGridView.CurrentCell = row.Cells["subjectColumn"];
        remindersDataGridView.FirstDisplayedScrollingRowIndex = row.Index;
        return true;
    }

    /// <summary>
    /// Diálogos del formulario (avisos, confirmaciones y errores): todo pasa
    /// por aquí. En la aplicación es un MessageBox con este formulario como
    /// dueño; las pruebas automatizadas de interfaz lo sustituyen por un
    /// delegado que responde sin bloquear.
    /// </summary>
    internal Func<string, string, MessageBoxButtons, MessageBoxIcon, DialogResult> Prompt { get; set; }

    /// <summary>
    /// Crea el formulario y carga los recordatorios guardados. Si la base de
    /// datos falla, la tabla queda vacía y los botones inactivos (constitución,
    /// punto 5).
    /// </summary>
    public ReminderForm() : this(null)
    {
    }

    /// <summary>
    /// Constructor con costura de pruebas: permite sustituir los diálogos
    /// antes de que la constructora cargue los datos, porque un error de
    /// base de datos durante la carga ya muestra un mensaje.
    /// </summary>
    internal ReminderForm(Func<string, string, MessageBoxButtons, MessageBoxIcon, DialogResult>? prompt)
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

        LoadReminders();
    }

    /// <summary>
    /// Carga en la tabla los recordatorios guardados, ordenados por fecha y
    /// hora ascendente, y prepara el desplegable «Cliente/Futuro cliente» con
    /// la unión de clientes y futuros clientes.
    /// </summary>
    private void LoadReminders()
    {
        List<Reminder> reminders;
        List<Client> clients;
        List<Contact> contacts;
        try
        {
            reminders = _dataStore!.LoadReminders();
            clients = _dataStore.LoadClients();
            contacts = _dataStore.LoadContacts();
        }
        catch (Exception ex)
        {
            ReportDataError(ex);
            return;
        }

        PopulateRelatedColumn(clients, contacts);
        List<Reminder> sorted = reminders.OrderBy(reminder => reminder.RemindAt).ToList();
        FillRows(sorted);

        _savedReminders = sorted;
        _dirty = false;
        _canSave = true;
        _lastDatabaseStamp = GetDatabaseStamp();

        // Sin recordatorio marcado hasta que el usuario haga clic en una fila
        // (skill «seleccionar-cliente»).
        _currentReminderId = null;
        remindersDataGridView.ClearSelection();
        UpdateCounter();
        UpdateButtonStates();
    }

    /// <summary>
    /// Vuelca en la tabla la lista indicada, en su orden, sustituyendo el
    /// contenido actual. La usan la carga y el Guardar correcto (que
    /// reordena por fecha y hora: decisión del usuario).
    /// </summary>
    private void FillRows(List<Reminder> reminders)
    {
        _loading = true;
        remindersDataGridView.Rows.Clear();
        foreach (Reminder reminder in reminders)
        {
            AddReminderRow(reminder);
        }
        _loading = false;
    }

    /// <summary>
    /// Rellena el desplegable «Cliente/Futuro cliente» con la unión de clientes
    /// y futuros clientes, y construye los mapas texto ↔ registro. Si dos
    /// registros se llaman igual, el segundo añade « (2)» para que el texto
    /// sea único (mismo criterio que TaskForm).
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
        _reminderService = new ReminderService(relatedRecords);
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
    /// Añade a la tabla una fila con los datos de un recordatorio guardado.
    /// </summary>
    private void AddReminderRow(Reminder reminder)
    {
        // Si el registro vinculado ya no existe, se muestra «—»; se añade a la
        // lista del desplegable para que la celda pueda pintarlo (mismo truco
        // que ClientForm con las provincias no oficiales).
        string related = _relatedTextById.TryGetValue(reminder.RelatedId, out string? text)
            ? text
            : "—";
        if (related == "—" && !relatedColumn.Items.Contains("—"))
        {
            relatedColumn.Items.Add("—");
        }

        int index = remindersDataGridView.Rows.Add(
            reminder.Subject,
            related,
            reminder.RemindAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
            StatusText(reminder.Status));

        // El identificador del recordatorio viaja en la fila sin mostrarse.
        remindersDataGridView.Rows[index].Tag = reminder.Id;
    }

    // Etiqueta en español del estado (constitución, punto 6); coincide con los
    // elementos del desplegable del Designer.
    private static string StatusText(ReminderStatus status) => status switch
    {
        ReminderStatus.Dismissed => "Descartado",
        _ => "Activo"
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
    /// Inserta una fila vacía como primera fila con el valor por defecto
    /// (Estado Activo), la activa para edición y coloca el cursor en el Asunto.
    /// </summary>
    private void NewButton_Click(object? sender, EventArgs e)
    {
        if (!_canSave || _activeRow is not null)
        {
            return;
        }

        remindersDataGridView.Rows.Insert(0, 1);
        System.Windows.Forms.DataGridViewRow row = remindersDataGridView.Rows[0];
        row.Cells["statusColumn"].Value = DefaultStatusText;

        ActivateRowEditing(row);

        // Selecciona la fila nueva y entra a editar la celda Asunto.
        remindersDataGridView.CurrentCell = row.Cells["subjectColumn"];
        remindersDataGridView.BeginEdit(true);
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

        // Durante la edición, el recordatorio actual es el de esta fila (skill
        // «seleccionar-cliente», regla 6).
        if (row.Tag is Guid activeId)
        {
            _currentReminderId = activeId;
        }

        // Las demás filas quedan deshabilitadas: atenuadas en gris y sin
        // selección posible mientras dure la edición (skill «modificar»).
        foreach (System.Windows.Forms.DataGridViewRow other in remindersDataGridView.Rows)
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
        foreach (System.Windows.Forms.DataGridViewRow gridRow in remindersDataGridView.Rows)
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
        foreach (System.Windows.Forms.DataGridViewColumn column in remindersDataGridView.Columns)
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
    /// Descarta la edición en curso: una fila nueva se elimina de la tabla y
    /// una modificada volverá a los valores de la instantánea tomada al
    /// activarla.
    /// </summary>
    private void CancelButton_Click(object? sender, EventArgs e)
    {
        if (_activeRow is null)
        {
            return;
        }

        // Confirma la celda en edición antes de restaurar.
        remindersDataGridView.EndEdit();

        System.Windows.Forms.DataGridViewRow row = _activeRow;

        if (row.Tag is not Guid)
        {
            // Fila creada con Nuevo y aún no guardada: se elimina.
            remindersDataGridView.Rows.Remove(row);
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
        EnsureCurrentReminderExists();

        EndEditingSession();
        ClearErrorMarks();

        // Recalcula si quedan otros cambios sin guardar (comparación con la
        // última copia guardada) para que el aviso al cerrar nunca mienta.
        _dirty = GridDiffersFromSaved();
    }

    /// <summary>
    /// Activa las celdas del recordatorio marcado para reescribir las que se
    /// quieran (skill «modificar»).
    /// </summary>
    private void ModifyButton_Click(object? sender, EventArgs e)
    {
        if (!_canSave || _activeRow is not null)
        {
            return;
        }

        // Recordatorio marcado con un clic en cualquier celda de su fila
        // (skill «seleccionar-cliente»): la fila se localiza por identificador.
        System.Windows.Forms.DataGridViewRow? row = FindRowById(_currentReminderId);
        if (row is null)
        {
            Prompt(
                "Selecciona un recordatorio de la tabla.",
                "CRM1 — Modificar",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ActivateRowEditing(row);

        // Coloca el cursor en la primera celda de la fila.
        remindersDataGridView.CurrentCell = row.Cells["subjectColumn"];
        remindersDataGridView.BeginEdit(true);
    }

    /// <summary>
    /// Elimina el recordatorio marcado tras pedir confirmación con su nombre
    /// (asunto y registro vinculado). La eliminación es en memoria: solo se
    /// hace permanente al pulsar Guardar (skill «borrar»; constitución,
    /// punto 5).
    /// </summary>
    private void DeleteButton_Click(object? sender, EventArgs e)
    {
        if (!_canSave || _activeRow is not null)
        {
            return;
        }

        // Recordatorio marcado con un clic (skill «seleccionar-cliente»).
        System.Windows.Forms.DataGridViewRow? row = FindRowById(_currentReminderId);
        if (row is null)
        {
            Prompt(
                "Selecciona un recordatorio de la tabla.",
                "CRM1 — Eliminar",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Confirmación antes de eliminar, identificando el recordatorio por
        // su asunto (entrecomillado por ser texto libre) y su registro
        // vinculado (specs/007 §4).
        DialogResult result = Prompt(
            $"¿Seguro que quieres eliminar el recordatorio {RowDisplayName(row)}?\n\n" +
            "Se quitará de la tabla y se eliminará por completo de la base de datos al pulsar Guardar.",
            "CRM1 — Eliminar",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        // Eliminación en memoria: solo Guardar escribe en disco.
        remindersDataGridView.Rows.Remove(row);

        // Si la fila eliminada era la marcada, la marca se limpia (regla 5).
        EnsureCurrentReminderExists();

        // Queda un cambio sin guardar pendiente de Guardar.
        _dirty = true;

        // El contador debe reflejar la fila retirada también sin edición
        // abierta (EndEditingSession vuelve pronto si no hay sesión).
        UpdateCounter();
    }

    /// <summary>
    /// Nombre visible de una fila de recordatorio: su asunto entrecomillado y
    /// el registro vinculado, para identificarla sin ambigüedad.
    /// </summary>
    private static string RowDisplayName(System.Windows.Forms.DataGridViewRow row)
    {
        string subject = CellText(row, "subjectColumn");
        string related = CellText(row, "relatedColumn");

        if (subject.Length > 0 && related.Length > 0)
        {
            return $"\"{subject}\" de {related}";
        }

        return subject.Length > 0 ? $"\"{subject}\"" : "este recordatorio";
    }

    /// <summary>
    /// Valida toda la tabla y, si todo es correcto, guarda los recordatorios
    /// en la base de datos y reordena la tabla por fecha y hora (decisión del
    /// usuario) antes de confirmar (specs/007).
    /// </summary>
    private void SaveButton_Click(object? sender, EventArgs e)
    {
        if (!_canSave)
        {
            return;
        }

        // Confirma la celda en edición para leer su último valor.
        remindersDataGridView.EndEdit();

        // Limpia las marcas rojas de una validación anterior.
        ClearErrorMarks();

        var errors = new List<(int Row, ReminderValidationError Error)>();
        var reminders = new List<Reminder>();
        var rowIndexes = new List<int>();

        for (int i = 0; i < remindersDataGridView.Rows.Count; i++)
        {
            System.Windows.Forms.DataGridViewRow row = remindersDataGridView.Rows[i];

            // Las filas totalmente vacías se ignoran (p. ej. un Nuevo por
            // accidente); el Estado Activo por defecto no cuenta como contenido.
            if (IsEmptyRow(row))
            {
                continue;
            }

            // Recorta los valores antes de validar y guardar.
            NormalizeRowCells(row);

            Reminder reminder = MapRowToReminder(row, out ReminderValidationError? error);
            if (error is not null)
            {
                errors.Add((i, error));
                continue;
            }

            reminders.Add(reminder);
            rowIndexes.Add(i);
        }

        // Reglas de negocio fuera del formulario (constitución, punto 3).
        foreach (ReminderRowValidationError rowError in _reminderService!.ValidateReminders(reminders))
        {
            errors.Add((rowIndexes[rowError.RowIndex], rowError.Error));
        }

        if (errors.Count > 0)
        {
            ShowValidationErrors(errors);
            return;
        }

        // Asigna identificadores a las filas que aún no tienen recordatorio guardado.
        for (int i = 0; i < reminders.Count; i++)
        {
            if (reminders[i].Id == Guid.Empty)
            {
                reminders[i].Id = Guid.NewGuid();
            }
            remindersDataGridView.Rows[rowIndexes[i]].Tag = reminders[i].Id;
        }

        try
        {
            _dataStore!.SaveReminders(reminders);
        }
        catch (Exception ex)
        {
            // Un fallo de escritura nunca se presenta como éxito (constitución, punto 5).
            Prompt(
                "No se han podido guardar los recordatorios.\n\n" + ex.Message,
                "CRM1 — Error al guardar",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _savedReminders = reminders;
        _dirty = false;
        _lastDatabaseStamp = GetDatabaseStamp();

        // La marca del registro actual pasa al identificador de la fila
        // editada (skill «seleccionar-cliente», regla 6); se captura antes de
        // reordenar porque FillRows reconstruye todas las filas.
        Guid? editedId = _activeRow is not null && _activeRow.Tag is Guid savedId ? savedId : _currentReminderId;

        // Todo vuelve a solo lectura y la tabla se reordena por fecha y hora
        // ascendente (decisión del usuario): la vista queda siempre de agenda.
        EndEditingSession();
        FillRows(reminders.OrderBy(reminder => reminder.RemindAt).ToList());
        _currentReminderId = editedId;

        // Tras el reordenado, la marca (y su borde) vuelve a la fila editada,
        // esté donde esté; si ya no existe, se limpia (regla 5).
        if (FindRowById(editedId) is { } editedRow)
        {
            editedRow.Selected = true;
        }
        else
        {
            EnsureCurrentReminderExists();
        }

        // Sin sesión de edición (p. ej. solo se eliminó una fila),
        // EndEditingSession vuelve pronto: el contador se refresca aquí.
        UpdateCounter();

        Prompt(
            $"Se han guardado {reminders.Count} recordatorio(s) correctamente.",
            "CRM1 — Guardar",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>
    /// Convierte una fila en un recordatorio; los errores de forma (vínculo,
    /// estado, fecha y hora) se devuelven como error de validación.
    /// </summary>
    private Reminder MapRowToReminder(System.Windows.Forms.DataGridViewRow row, out ReminderValidationError? error)
    {
        error = null;

        // Asunto: texto libre; vacío se valida en el servicio.
        string subject = CellText(row, "subjectColumn");

        // Vínculo: el texto del desplegable se traduce a (Id, tipo).
        Guid relatedId = Guid.Empty;
        TaskRelatedType relatedType = TaskRelatedType.Client;
        string relatedText = CellText(row, "relatedColumn");
        if (relatedText.Length == 0 || relatedText == "—" || !_relatedByText.TryGetValue(relatedText, out (Guid Id, TaskRelatedType Type) related))
        {
            error = new ReminderValidationError
            {
                Field = nameof(Reminder.RelatedId),
                Message = "Debes elegir un cliente o futuro cliente."
            };
        }
        else
        {
            relatedId = related.Id;
            relatedType = related.Type;
        }

        // Estado: desplegable con dos valores.
        string statusText = CellText(row, "statusColumn");
        ReminderStatus status = statusText switch
        {
            "Descartado" => ReminderStatus.Dismissed,
            "Activo" => ReminderStatus.Active,
            _ => ReminderStatus.Active
        };
        if (error is null && statusText is not ("Activo" or "Descartado"))
        {
            error = new ReminderValidationError
            {
                Field = nameof(Reminder.Status),
                Message = "El estado no es válido."
            };
        }

        // Fecha y hora en formato dd/MM/aaaa HH:mm.
        DateTime remindAt = default;
        if (error is null)
        {
            string dateText = CellText(row, "remindAtColumn");
            if (dateText.Length == 0)
            {
                error = new ReminderValidationError
                {
                    Field = nameof(Reminder.RemindAt),
                    Message = "La celda está vacía."
                };
            }
            else if (!DateTime.TryParseExact(dateText, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out remindAt))
            {
                error = new ReminderValidationError
                {
                    Field = nameof(Reminder.RemindAt),
                    Message = "La fecha no es válida (dd/mm/aaaa hh:mm)."
                };
            }
        }

        return new Reminder
        {
            Id = row.Tag is Guid id ? id : Guid.Empty,
            Subject = subject,
            RemindAt = remindAt,
            Status = status,
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
    /// Indica si todas las celdas de la fila están vacías. El Estado Activo
    /// por defecto de una fila nueva no cuenta como contenido, para poder
    /// descartar Nuevos por accidente (mismo criterio que TaskForm con su
    /// Pendiente/Media).
    /// </summary>
    private bool IsEmptyRow(System.Windows.Forms.DataGridViewRow row)
    {
        foreach (System.Windows.Forms.DataGridViewCell cell in row.Cells)
        {
            string value = cell.Value?.ToString()?.Trim() ?? string.Empty;

            // El valor por defecto del Estado se ignora.
            if (cell.OwningColumn.Name == "statusColumn" && value == DefaultStatusText)
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
    private void ShowValidationErrors(List<(int Row, ReminderValidationError Error)> errors)
    {
        foreach (var (row, reminderError) in errors)
        {
            string? columnName = ColumnNameFor(reminderError.Field);
            if (columnName is not null)
            {
                _errorCells.Add(remindersDataGridView.Rows[row].Cells[columnName]);
            }
        }
        remindersDataGridView.Invalidate();

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
        remindersDataGridView.Invalidate();
    }

    /// <summary>
    /// Nombre de columna de la tabla para un campo del modelo.
    /// </summary>
    private static string? ColumnNameFor(string field) => field switch
    {
        nameof(Reminder.Subject) => "subjectColumn",
        nameof(Reminder.RelatedId) => "relatedColumn",
        nameof(Reminder.RemindAt) => "remindAtColumn",
        nameof(Reminder.Status) => "statusColumn",
        _ => null
    };

    /// <summary>
    /// Indica si la tabla (filas no vacías) difiere de la última copia
    /// guardada, comparando por identificador para no depender del orden.
    /// </summary>
    private bool GridDiffersFromSaved()
    {
        var current = new List<Reminder>();
        for (int i = 0; i < remindersDataGridView.Rows.Count; i++)
        {
            System.Windows.Forms.DataGridViewRow row = remindersDataGridView.Rows[i];
            if (IsEmptyRow(row))
            {
                continue;
            }
            current.Add(MapRowToReminder(row, out _));
        }

        if (current.Count != _savedReminders.Count)
        {
            return true;
        }

        var savedById = new Dictionary<Guid, Reminder>();
        foreach (Reminder reminder in _savedReminders)
        {
            savedById[reminder.Id] = reminder;
        }

        foreach (Reminder reminder in current)
        {
            if (!savedById.TryGetValue(reminder.Id, out Reminder? saved) || !RemindersEqual(reminder, saved))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Compara dos recordatorios campo a campo.
    /// </summary>
    private static bool RemindersEqual(Reminder a, Reminder b)
    {
        return string.Equals(a.Subject, b.Subject, StringComparison.Ordinal)
            && a.RemindAt == b.RemindAt
            && a.Status == b.Status
            && a.RelatedId == b.RelatedId
            && a.RelatedType == b.RelatedType;
    }

    /// <summary>
    /// Actualiza el contador del listado: sin filtro local — la búsqueda se
    /// hace desde el formulario principal (specs/004) — todas las filas están
    /// visibles (specs/007).
    /// </summary>
    private void UpdateCounter()
    {
        if (_loading)
        {
            return;
        }

        int totalCount = 0;
        foreach (System.Windows.Forms.DataGridViewRow row in remindersDataGridView.Rows)
        {
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
    /// Al volver a mostrar el módulo, recarga los recordatorios si la base de
    /// datos cambió en disco y aquí no hay nada pendiente (mismo criterio que
    /// ClientForm y TaskForm).
    /// </summary>
    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);

        if (Visible && _canSave && !_dirty && _activeRow is null
            && GetDatabaseStamp() != _lastDatabaseStamp)
        {
            LoadReminders();
        }
    }

    /// <summary>
    /// Muestra el error de datos y desactiva la gestión (constitución, punto 5).
    /// </summary>
    private void ReportDataError(Exception ex)
    {
        Prompt(
            "No se pudo leer la base de datos de recordatorios (Data/crm1.db).\n\n" +
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
    private void RemindersDataGridView_CellValueChanged(object? sender, System.Windows.Forms.DataGridViewCellEventArgs e)
    {
        if (!_loading && e.RowIndex >= 0)
        {
            _dirty = true;
        }

        // Al corregir una celda marcada, su borde rojo desaparece al momento.
        if (e.RowIndex >= 0 && e.ColumnIndex >= 0
            && _errorCells.Remove(remindersDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex]))
        {
            remindersDataGridView.Invalidate();
        }
    }

    /// <summary>
    /// Marca que hay cambios sin guardar cuando se añade una fila; toda fila
    /// nueva nace en solo lectura (la activa se desbloquea al activarse).
    /// </summary>
    private void RemindersDataGridView_RowsAdded(object? sender, System.Windows.Forms.DataGridViewRowsAddedEventArgs e)
    {
        if (!_loading)
        {
            _dirty = true;
        }

        if (e.RowIndex >= 0)
        {
            remindersDataGridView.Rows[e.RowIndex].ReadOnly = true;
        }
    }

    /// <summary>
    /// Dibuja el borde rojo alrededor de las celdas con errores de validación.
    /// Las celdas permanecen siempre blancas para escribir.
    /// </summary>
    private void RemindersDataGridView_CellPainting(object? sender, System.Windows.Forms.DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        System.Windows.Forms.DataGridViewCell cell = remindersDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex];
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
    private void RemindersDataGridView_DataError(object? sender, System.Windows.Forms.DataGridViewDataErrorEventArgs e)
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
    private void RemindersDataGridView_SelectionChanged(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        System.Windows.Forms.DataGridViewRow? selected = remindersDataGridView.SelectedRows.Count > 0
            ? remindersDataGridView.SelectedRows[0]
            : null;

        if (_activeRow is not null && _activeRow.Index >= 0
            && !_restoringSelection && selected != _activeRow)
        {
            _restoringSelection = true;
            try
            {
                remindersDataGridView.ClearSelection();

                // Devuelve también la celda actual a la fila en edición,
                // manteniendo la columna en la que estaba el usuario.
                int column = remindersDataGridView.CurrentCell?.ColumnIndex ?? 0;
                if (column >= 0 && column < _activeRow.Cells.Count)
                {
                    remindersDataGridView.CurrentCell = _activeRow.Cells[column];
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

        // La fila seleccionada marca el recordatorio actual (clic o teclado);
        // sin selección no hay recordatorio actual, nunca un valor inventado
        // (regla 1).
        _currentReminderId = selected?.Tag is Guid reminderId ? reminderId : null;
    }

    /// <summary>
    /// Busca la fila del recordatorio con el identificador indicado (rastreo
    /// por Id, nunca por índice: admite ordenar o recargar la tabla).
    /// </summary>
    private System.Windows.Forms.DataGridViewRow? FindRowById(Guid? reminderId)
    {
        if (reminderId is null)
        {
            return null;
        }

        foreach (System.Windows.Forms.DataGridViewRow row in remindersDataGridView.Rows)
        {
            if (row.Tag is Guid id && id == reminderId)
            {
                return row;
            }
        }

        return null;
    }

    /// <summary>
    /// Limpia la marca de recordatorio actual si su fila ya no existe (regla 5
    /// de la skill «seleccionar-cliente»: sin identificadores huérfanos).
    /// </summary>
    private void EnsureCurrentReminderExists()
    {
        if (_currentReminderId is not null && FindRowById(_currentReminderId) is null)
        {
            _currentReminderId = null;
        }
    }

    /// <summary>
    /// Dibuja el borde grueso alrededor de la fila seleccionada, igual que en
    /// los demás módulos. Las celdas siguen siempre blancas.
    /// </summary>
    private void RemindersDataGridView_RowPostPaint(object? sender, System.Windows.Forms.DataGridViewRowPostPaintEventArgs e)
    {
        if (e.RowIndex < 0 || !remindersDataGridView.Rows[e.RowIndex].Selected)
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
}
