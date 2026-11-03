using System.Globalization;
using Crm1.WindowsForms.Models;
using Crm1.WindowsForms.Services;

namespace Crm1.WindowsForms.Forms;

/// <summary>
/// Formulario del módulo de futuros clientes (specs/003-formulario-futuros-clientes.md).
/// Como un futuro cliente no siempre tiene todos los datos, las celdas pueden
/// quedar vacías salvo el nombre y un medio de contacto (teléfono o correo).
/// La tabla es de solo lectura por defecto: Nuevo y Modificar activan una fila
/// para editarla y Guardar la persiste (patrón de la skill «modificar»).
/// La lógica de negocio vive en Services (constitución, punto 3).
/// </summary>
public partial class ContactForm : System.Windows.Forms.Form
{
    // Color del borde que marca las celdas con errores de validación.
    private static readonly System.Drawing.Color ErrorBorderColor = System.Drawing.Color.FromArgb(220, 38, 38);

    // Color y grosor del borde que marca la fila seleccionada (futuro cliente
    // actual, skill «seleccionar-cliente»); más grueso que el de errores.
    private static readonly System.Drawing.Color SelectionBorderColor = System.Drawing.Color.FromArgb(100, 116, 139);
    private const float SelectionBorderWidth = 3F;

    // Color del texto de las filas deshabilitadas mientras hay edición abierta.
    private static readonly System.Drawing.Color DisabledRowTextColor = System.Drawing.Color.FromArgb(156, 163, 175);

    // Texto del estado por defecto con el que nace cada fila nueva.
    private const string DefaultStatusText = "Pendiente";

    private readonly IDataStore _dataStore;
    private readonly PostalCodeCatalog? _catalog;
    private readonly ContactService? _contactService;

    // Falso si el catálogo de CP, clients.json o contacts.json no se pudieron
    // cargar: todos los botones de gestión quedan inactivos (constitución, punto 5).
    private bool _canSave;

    // Fila activa para edición (rastreada por referencia); null si no hay sesión.
    private System.Windows.Forms.DataGridViewRow? _activeRow;

    // Instantánea de los valores de la fila activa al empezar la edición,
    // para restaurarlos si se pulsa Cancelar.
    private List<object?> _activeRowSnapshot = new();

    // Última copia guardada de los futuros clientes, para recalcular si quedan cambios.
    private List<Contact> _savedContacts = new();

    // Celdas con errores de validación: reciben un borde rojo alrededor.
    private readonly HashSet<System.Windows.Forms.DataGridViewCell> _errorCells = new();

    // Verdadero mientras la tabla se rellena desde disco (no son cambios del usuario).
    private bool _loading;

    // Guarda anti-reentrada al devolver la selección a la fila en edición.
    private bool _restoringSelection;

    // Verdadero cuando hay cambios sin guardar.
    private bool _dirty;

    // Conversiones a cliente pendientes de guardar (specs/003, sección 7):
    // clientes listos para dar de alta en clients.json al pulsar Guardar.
    private readonly List<Client> _pendingConversions = new();

    // Futuros clientes convertidos en memoria: permiten deshacer con Cancelar
    // devolviéndolos a la tabla mientras no se guarde.
    private readonly List<Contact> _convertedContacts = new();

    /// <summary>
    /// Verdadero cuando la tabla tiene cambios sin guardar. Lo consulta MainForm
    /// antes de cerrar la aplicación porque ContactForm va embebido (specs/003).
    /// </summary>
    public bool HasUnsavedChanges => _dirty;

    /// <summary>
    /// Diálogos del formulario (avisos, confirmaciones y errores): todo pasa
    /// por aquí. En la aplicación es un MessageBox con este formulario como
    /// dueño; las pruebas automatizadas de interfaz lo sustituyen por un
    /// delegado que responde sin bloquear.
    /// </summary>
    internal Func<string, string, MessageBoxButtons, MessageBoxIcon, DialogResult> Prompt { get; set; }

    // Futuro cliente actual marcado por el usuario (skill «seleccionar-cliente»):
    // rastreo por identificador, nunca por índice.
    private Guid? _currentContactId;

    /// <summary>
    /// Identificador del futuro cliente actual (fila marcada con un clic en
    /// cualquier celda), o null si no hay selección. Skill «seleccionar-cliente».
    /// </summary>
    public Guid? CurrentContactId => _currentContactId;

    /// <summary>
    /// Futuro cliente actual reconstruido de la fila marcada, o null si no hay marca.
    /// </summary>
    public Contact? CurrentContact => FindRowById(_currentContactId) is { } row
        ? MapRowToContact(row, out _)
        : null;

    /// <summary>
    /// Verdadero mientras hay una fila en edición (Nuevo o Modificar). Lo
    /// consulta la búsqueda global antes de navegar hasta un futuro cliente.
    /// </summary>
    public bool IsEditing => _activeRow is not null;

    /// <summary>
    /// Marca como futuro cliente actual el indicado y desplaza la tabla hasta
    /// su fila (búsqueda global, specs/004). Devuelve false si no está en la
    /// tabla o hay una edición en curso.
    /// </summary>
    public bool SelectContactById(Guid contactId)
    {
        if (_activeRow is not null)
        {
            return false;
        }

        System.Windows.Forms.DataGridViewRow? row = FindRowById(contactId);
        if (row is null)
        {
            return false;
        }

        contactsDataGridView.CurrentCell = row.Cells["firstNameColumn"];
        contactsDataGridView.FirstDisplayedScrollingRowIndex = row.Index;
        return true;
    }

    /// <summary>
    /// Crea el formulario, carga el catálogo de códigos postales, los documentos
    /// de los clientes guardados (regla cruzada de specs/003) y los futuros
    /// clientes. Si algo falla, todos los botones de gestión quedan inactivos
    /// para no sobrescribir datos (constitución, punto 5).
    /// </summary>
    public ContactForm()
    {
        InitializeComponent();

        // En la aplicación los diálogos son MessageBox con este formulario
        // como dueño (costura de pruebas: ver el comentario de la propiedad).
        Prompt = (text, caption, buttons, icon) =>
            MessageBox.Show(this, text, caption, buttons, icon);

        PostalCodeCatalog? catalog = null;
        try
        {
            string catalogPath = Path.Combine(AppContext.BaseDirectory, "Data", "postal-codes-es.json");
            catalog = new PostalCodeCatalog(catalogPath);
        }
        catch (Exception ex)
        {
            Prompt(
                "No se pudo cargar el archivo de códigos postales (Data/postal-codes-es.json).\n\n" +
                "Los botones de gestión se han desactivado porque no se pueden validar los datos.\n\n" +
                ex.Message,
                "CRM1 — Error de datos",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        _catalog = catalog;
        _dataStore = new JsonDataStore();

        // Documentos de los clientes guardados: un documento de cliente no puede
        // registrarse como futuro cliente. Si el archivo no se puede leer, no se
        // puede aplicar la regla y los botones quedan inactivos.
        List<string> clientDocuments = new();
        if (_catalog is not null)
        {
            try
            {
                clientDocuments = _dataStore.LoadClients().Select(client => client.TaxId).ToList();
            }
            catch (Exception ex)
            {
                Prompt(
                    "No se pudo leer el archivo de clientes (Data/clients.json), necesario para\n" +
                    "comprobar que un documento de un cliente no se usa como futuro cliente.\n\n" +
                    "Los botones de gestión se han desactivado para no omitir la comprobación.\n\n" +
                    ex.Message,
                    "CRM1 — Error de datos",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        _contactService = _catalog is null ? null : new ContactService(_catalog, clientDocuments);
        _canSave = _contactService is not null;

        if (_catalog is not null)
        {
            PopulateProvinceColumn();
        }

        LoadContacts();
        UpdateButtonStates();
    }

    /// <summary>
    /// Rellena el desplegable de provincias con las provincias oficiales del catálogo.
    /// </summary>
    private void PopulateProvinceColumn()
    {
        provinceColumn.Items.Clear();
        foreach (string province in _catalog!.Provinces)
        {
            provinceColumn.Items.Add(province);
        }
    }

    /// <summary>
    /// Actualiza los botones según el estado (normal o edición) y si se pueden
    /// validar y guardar datos. Convertir y el filtro quedan bloqueados durante
    /// la edición, como el resto de la botonera.
    /// </summary>
    private void UpdateButtonStates()
    {
        bool editing = _activeRow is not null;

        deleteButton.Enabled = _canSave && !editing;
        newButton.Enabled = _canSave && !editing;
        modifyButton.Enabled = _canSave && !editing;
        saveButton.Enabled = _canSave;
        cancelButton.Enabled = _canSave && editing;
        convertButton.Enabled = _canSave && !editing;

        // La barra de filtro también queda bloqueada mientras hay edición.
        searchTextBox.Enabled = _canSave && !editing;
        statusFilterComboBox.Enabled = _canSave && !editing;
    }

    /// <summary>
    /// Activa una fila para edición (solo ella pierde la solo lectura) y toma una
    /// instantánea de sus valores para poder cancelar.
    /// </summary>
    private void ActivateRowEditing(System.Windows.Forms.DataGridViewRow row)
    {
        // Cierra cualquier sesión anterior dejando la fila en solo lectura.
        EndEditingSession();

        // Instantánea de los valores actuales para Cancelar.
        _activeRowSnapshot = row.Cells.Cast<System.Windows.Forms.DataGridViewCell>()
            .Select(cell => (object?)cell.Value)
            .ToList();

        _activeRow = row;
        row.ReadOnly = false;

        // Durante la edición, el futuro cliente actual es el de esta fila (skill
        // «seleccionar-cliente», regla 6).
        if (row.Tag is Guid activeId)
        {
            _currentContactId = activeId;
        }

        // Las demás filas quedan deshabilitadas: atenuadas en gris y sin
        // selección posible mientras dure la edición (specs/003).
        foreach (System.Windows.Forms.DataGridViewRow other in contactsDataGridView.Rows)
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
        foreach (System.Windows.Forms.DataGridViewRow gridRow in contactsDataGridView.Rows)
        {
            gridRow.DefaultCellStyle.ForeColor = System.Drawing.SystemColors.ControlText;
        }
        SetColumnsSortable(true);
        UpdateButtonStates();
        ApplyFilter();
    }

    /// <summary>
    /// Activa o desactiva la ordenación por cabeceras (bloqueada durante la edición
    /// para no descuadrar la fila abierta). Las columnas de desplegable nunca ordenan.
    /// </summary>
    private void SetColumnsSortable(bool sortable)
    {
        foreach (System.Windows.Forms.DataGridViewColumn column in contactsDataGridView.Columns)
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
    /// Carga en la tabla los futuros clientes guardados en disco. Si el archivo
    /// está dañado, todos los botones de gestión quedan inactivos para no
    /// sobrescribir los datos.
    /// </summary>
    private void LoadContacts()
    {
        List<Contact> contacts;
        try
        {
            contacts = _dataStore.LoadContacts();
        }
        catch (Exception ex)
        {
            Prompt(
                "No se pudo leer el archivo de futuros clientes (Data/contacts.json).\n\n" +
                "Los botones de gestión se han desactivado para no sobrescribir los datos.\n\n" +
                ex.Message,
                "CRM1 — Error de datos",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _canSave = false;
            return;
        }

        _loading = true;
        foreach (Contact contact in contacts)
        {
            AddContactRow(contact);
        }
        _loading = false;
        _savedContacts = contacts;
        _dirty = false;
        _pendingConversions.Clear();
        _convertedContacts.Clear();

        // Sin futuro cliente actual hasta que el usuario marque una fila con un
        // clic (skill «seleccionar-cliente»).
        _currentContactId = null;
        contactsDataGridView.ClearSelection();

        // Aplica el filtro vigente y actualiza el contador del listado.
        ApplyFilter();
    }

    /// <summary>
    /// Aplica el filtro del listado (specs/003): quedan visibles las filas que
    /// cumplen el texto buscado (parcial, sin mayúsculas ni tildes, en todas
    /// las columnas) y el estado del desplegable; el contador muestra cuántas
    /// se ven de cuántas hay.
    /// </summary>
    private void ApplyFilter()
    {
        if (_loading)
        {
            return;
        }

        ContactStatus? status = SelectedStatusFilter();
        string text = searchTextBox.Text;

        // Se recuerda el futuro cliente actual porque al soltar la celda o la
        // selección el evento SelectionChanged limpia la marca; se restaura al
        // final si su fila sigue visible.
        Guid? currentId = _currentContactId;

        int visibleCount = 0;
        int totalCount = 0;
        foreach (System.Windows.Forms.DataGridViewRow row in contactsDataGridView.Rows)
        {
            // Las filas vacías (solo posibles con edición abierta) se dejan ver.
            if (IsEmptyRow(row))
            {
                row.Visible = true;
                continue;
            }

            totalCount++;
            bool matches = ContactService.MatchesFilter(MapRowToContact(row, out _), text, status);

            // La celda actual se suelta antes de ocultar su fila: el control no
            // permite ocultar la fila que la contiene.
            if (!matches && contactsDataGridView.CurrentCell?.OwningRow == row)
            {
                contactsDataGridView.CurrentCell = null;
                contactsDataGridView.ClearSelection();
            }

            row.Visible = matches;
            if (matches)
            {
                visibleCount++;
            }
        }

        counterLabel.Text = $"Mostrando {visibleCount} de {totalCount}";

        // Si el futuro cliente actual quedó oculto por el filtro, la marca se
        // retira (volverá a marcarse con un clic cuando vuelva a verse); si
        // sigue visible, conserva la selección.
        System.Windows.Forms.DataGridViewRow? currentRow = FindRowById(currentId);
        if (currentRow is null || !currentRow.Visible)
        {
            contactsDataGridView.ClearSelection();
            _currentContactId = null;
        }
        else
        {
            _currentContactId = currentId;
            _restoringSelection = true;
            try
            {
                currentRow.Selected = true;
            }
            finally
            {
                _restoringSelection = false;
            }
        }
    }

    /// <summary>
    /// Estado seleccionado en el desplegable del filtro; null = Todos.
    /// </summary>
    private ContactStatus? SelectedStatusFilter() => statusFilterComboBox.SelectedIndex switch
    {
        1 => ContactStatus.Pending,
        2 => ContactStatus.Discarded,
        _ => null
    };

    /// <summary>
    /// Vuelve a aplicar el filtro al cambiar el texto buscado (búsqueda en vivo).
    /// </summary>
    private void SearchTextBox_TextChanged(object? sender, EventArgs e) => ApplyFilter();

    /// <summary>
    /// Vuelve a aplicar el filtro al cambiar el estado del desplegable.
    /// </summary>
    private void StatusFilterComboBox_SelectedIndexChanged(object? sender, EventArgs e) => ApplyFilter();

    /// <summary>
    /// Añade a la tabla una fila con los datos de un futuro cliente guardado.
    /// </summary>
    private void AddContactRow(Contact contact)
    {
        // Si la provincia guardada no está en la lista oficial se añade para poder
        // mostrarla; la validación de Guardar la detectará si no es válida.
        string province = contact.Province;
        if (!string.IsNullOrWhiteSpace(province) && !provinceColumn.Items.Contains(province))
        {
            provinceColumn.Items.Add(province);
        }

        // La fecha puede faltar (dato opcional): la celda se queda vacía.
        string registrationDate = contact.RegistrationDate == default
            ? string.Empty
            : contact.RegistrationDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

        int index = contactsDataGridView.Rows.Add(
            contact.FirstName,
            contact.LastName1,
            contact.LastName2,
            contact.TaxId,
            contact.Phone,
            contact.Email,
            contact.Address,
            contact.PostalCode,
            contact.City,
            province,
            contact.Status == ContactStatus.Discarded ? "Descartado" : "Pendiente",
            registrationDate,
            contact.Notes);

        // El identificador del futuro cliente viaja en la fila sin mostrarse.
        contactsDataGridView.Rows[index].Tag = contact.Id;
    }

    /// <summary>
    /// Inserta una fila vacía como primera fila (con estado Pendiente), la activa
    /// para edición y comienza a editar el nombre.
    /// </summary>
    private void NewButton_Click(object? sender, EventArgs e)
    {
        if (!_canSave || _activeRow is not null)
        {
            return;
        }

        contactsDataGridView.Rows.Insert(0, 1);

        // La fila nueva nace con estado Pendiente (specs/003).
        contactsDataGridView.Rows[0].Cells["statusColumn"].Value = DefaultStatusText;

        ActivateRowEditing(contactsDataGridView.Rows[0]);

        // Selecciona la fila nueva y entra a editar la celda Nombre.
        contactsDataGridView.CurrentCell = contactsDataGridView.Rows[0].Cells["firstNameColumn"];
        contactsDataGridView.BeginEdit(true);
    }

    /// <summary>
    /// Activa las celdas del futuro cliente seleccionado para reescribir las que
    /// se quieran.
    /// </summary>
    private void ModifyButton_Click(object? sender, EventArgs e)
    {
        if (!_canSave || _activeRow is not null)
        {
            return;
        }

        // Futuro cliente actual marcado con un clic en cualquier celda de su fila
        // (skill «seleccionar-cliente»): la fila se localiza por identificador.
        System.Windows.Forms.DataGridViewRow? row = FindRowById(_currentContactId);
        if (row is null)
        {
            Prompt(
                "Selecciona un futuro cliente de la tabla.",
                "CRM1 — Modificar",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ActivateRowEditing(row);

        // Coloca el cursor en la primera celda de la fila.
        contactsDataGridView.CurrentCell = row.Cells["firstNameColumn"];
        contactsDataGridView.BeginEdit(true);
    }

    /// <summary>
    /// Elimina el futuro cliente actual de la tabla tras pedir confirmación con
    /// su nombre. La eliminación es en memoria: solo se hace permanente al
    /// pulsar Guardar (skill «borrar»; constitución, punto 5).
    /// </summary>
    private void DeleteButton_Click(object? sender, EventArgs e)
    {
        if (!_canSave || _activeRow is not null)
        {
            return;
        }

        // Futuro cliente actual marcado con un clic (skill «seleccionar-cliente»).
        System.Windows.Forms.DataGridViewRow? row = FindRowById(_currentContactId);
        if (row is null)
        {
            Prompt(
                "Selecciona un futuro cliente de la tabla.",
                "CRM1 — Eliminar",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Confirmación antes de eliminar, identificando al futuro cliente por su nombre.
        DialogResult result = Prompt(
            $"¿Seguro que quieres eliminar a {RowDisplayName(row)}?\n\n" +
            "Se quitará de la tabla y se eliminará por completo del archivo al pulsar Guardar.",
            "CRM1 — Eliminar",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        // Eliminación en memoria: solo Guardar escribe en disco.
        contactsDataGridView.Rows.Remove(row);

        // Si la fila eliminada era la marcada, la marca se limpia (regla 5).
        EnsureCurrentContactExists();

        // Queda un cambio sin guardar pendiente de Guardar.
        _dirty = true;
    }

    /// <summary>
    /// Nombre visible de una fila: nombre y apellidos; en empresas, la razón
    /// social (los apellidos están vacíos).
    /// </summary>
    private static string RowDisplayName(System.Windows.Forms.DataGridViewRow row)
    {
        string name = string.Join(" ",
            new[]
            {
                CellText(row, "firstNameColumn"),
                CellText(row, "lastName1Column"),
                CellText(row, "lastName2Column")
            }.Where(part => part.Length > 0));

        return name.Length > 0 ? name : "este futuro cliente";
    }

    /// <summary>
    /// Convierte en cliente el futuro cliente actual (specs/003, sección 7):
    /// valida que cumpla todas las reglas de los clientes, pide doble
    /// confirmación y deja la conversión en memoria hasta pulsar Guardar.
    /// </summary>
    private void ConvertButton_Click(object? sender, EventArgs e)
    {
        if (!_canSave || _activeRow is not null)
        {
            return;
        }

        // Futuro cliente actual marcado con un clic (skill «seleccionar-cliente»).
        System.Windows.Forms.DataGridViewRow? row = FindRowById(_currentContactId);
        if (row is null)
        {
            Prompt(
                "Selecciona un futuro cliente de la tabla.",
                "CRM1 — Convertir en cliente",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Normaliza antes de validar, igual que al guardar.
        NormalizeRowCells(row);
        Contact contact = MapRowToContact(row, out _);
        string displayName = RowDisplayName(row);

        ConversionResult result = _contactService!.ConvertToClient(contact);
        if (!result.Success)
        {
            Prompt(
                $"{displayName} no puede convertirse en cliente todavía:\n\n" +
                string.Join("\n", result.Errors) +
                "\n\nCompleta sus datos en Futuros clientes y vuelve a intentarlo.",
                "CRM1 — Convertir en cliente",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Doble seguridad (specs/003): primera confirmación con el resumen.
        DialogResult first = Prompt(
            $"Vas a convertir a {displayName} en cliente con estos datos:\n\n" +
            ClientSummary(result.Client!) + "\n\n¿Continuar?",
            "CRM1 — Convertir en cliente",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (first != DialogResult.Yes)
        {
            return;
        }

        // Segunda confirmación: advierte de lo que ocurrirá al guardar.
        DialogResult second = Prompt(
            $"Al guardar, {displayName} se dará de alta en Clientes y desaparecerá de Futuros clientes.\n\n" +
            "Hasta entonces puedes deshacerlo con Cancelar o saliendo sin guardar.\n\n" +
            "¿Confirmas la conversión?",
            "CRM1 — Confirmar conversión",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (second != DialogResult.Yes)
        {
            return;
        }

        // Conversión en memoria: la fila se retira de la tabla y el alta se
        // escribe en clients.json al pulsar Guardar, nunca antes.
        _pendingConversions.Add(result.Client!);
        _convertedContacts.Add(contact);
        contactsDataGridView.Rows.Remove(row);
        EnsureCurrentContactExists();
        _dirty = true;
        ApplyFilter();
    }

    /// <summary>
    /// Resumen de los datos con los que se dará de alta un cliente convertido.
    /// </summary>
    private static string ClientSummary(Client client)
    {
        string name = string.Join(" ",
            new[] { client.FirstName, client.LastName1, client.LastName2 }.Where(part => part.Length > 0));

        var lines = new List<string> { $"Nombre: {name}", $"Documento: {client.TaxId}" };
        if (client.Phone.Length > 0)
        {
            lines.Add($"Teléfono: {client.Phone}");
        }
        if (client.Email.Length > 0)
        {
            lines.Add($"Correo: {client.Email}");
        }
        if (client.City.Length > 0)
        {
            lines.Add($"Ciudad: {client.City} ({client.Province})");
        }
        lines.Add($"Fecha de alta: {client.RegistrationDate:dd/MM/yyyy}");
        return string.Join("\n", lines);
    }

    /// <summary>
    /// Descarta la edición en curso: una fila nueva se elimina de la tabla y una
    /// modificada vuelve a los valores de la instantánea tomada al activarla.
    /// </summary>
    private void CancelButton_Click(object? sender, EventArgs e)
    {
        // Sin edición abierta, Cancelar deshace las conversiones pendientes
        // (tercera capa de seguridad de specs/003, sección 7).
        if (_activeRow is null)
        {
            if (_pendingConversions.Count == 0)
            {
                return;
            }

            DialogResult undo = Prompt(
                $"Hay {_pendingConversions.Count} conversión(es) a cliente pendientes de guardar.\n\n" +
                "¿Cancelarlas y devolver los futuros clientes a la tabla?",
                "CRM1 — Cancelar conversiones",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (undo != DialogResult.Yes)
            {
                return;
            }

            foreach (Contact contact in _convertedContacts)
            {
                AddContactRow(contact);
            }

            _pendingConversions.Clear();
            _convertedContacts.Clear();
            _dirty = GridDiffersFromSaved();
            EnsureCurrentContactExists();
            ApplyFilter();
            return;
        }

        // Confirma la celda en edición antes de restaurar.
        contactsDataGridView.EndEdit();

        System.Windows.Forms.DataGridViewRow row = _activeRow;

        if (row.Tag is not Guid)
        {
            // Fila creada con Nuevo y aún no guardada: se elimina.
            contactsDataGridView.Rows.Remove(row);
        }
        else
        {
            // Fila guardada antes: vuelve a los valores anteriores.
            for (int i = 0; i < row.Cells.Count && i < _activeRowSnapshot.Count; i++)
            {
                row.Cells[i].Value = _activeRowSnapshot[i];
            }
        }

        // Si la fila eliminada era la marcada, la marca se limpia (regla 5).
        EnsureCurrentContactExists();

        EndEditingSession();
        ClearErrorMarks();

        // Recalcula si quedan otros cambios sin guardar (comparación con la última
        // copia guardada) para que el aviso al cerrar nunca mienta.
        _dirty = GridDiffersFromSaved();
    }

    /// <summary>
    /// Valida toda la tabla y, si todo es correcto, guarda los futuros clientes
    /// en disco.
    /// </summary>
    private void SaveButton_Click(object? sender, EventArgs e)
    {
        if (!_canSave)
        {
            return;
        }

        // Confirma la celda en edición para leer su último valor.
        contactsDataGridView.EndEdit();

        // Limpia las marcas rojas de una validación anterior.
        ClearErrorMarks();

        var errors = new List<(int Row, ContactValidationError Error)>();
        var contacts = new List<Contact>();
        var rowIndexes = new List<int>();

        for (int i = 0; i < contactsDataGridView.Rows.Count; i++)
        {
            System.Windows.Forms.DataGridViewRow row = contactsDataGridView.Rows[i];

            // Las filas totalmente vacías se ignoran (p. ej. un Nuevo por accidente);
            // el estado Pendiente de la fila nueva no cuenta como dato.
            if (IsEmptyRow(row))
            {
                continue;
            }

            // Normaliza lo escrito y lo refleja en las celdas, para que se guarde
            // y se muestre exactamente lo validado.
            NormalizeRowCells(row);

            Contact contact = MapRowToContact(row, out ContactValidationError? error);
            if (error is not null)
            {
                errors.Add((i, error!));
                continue;
            }

            contacts.Add(contact);
            rowIndexes.Add(i);
        }

        // Reglas de negocio fuera del formulario (constitución, punto 3).
        foreach (ContactRowValidationError rowError in _contactService!.ValidateContacts(contacts))
        {
            errors.Add((rowIndexes[rowError.RowIndex], rowError.Error));
        }

        if (errors.Count > 0)
        {
            ShowValidationErrors(errors);
            return;
        }

        // Asigna identificadores a las filas que aún no tienen futuro cliente guardado.
        for (int i = 0; i < contacts.Count; i++)
        {
            if (contacts[i].Id == Guid.Empty)
            {
                contacts[i].Id = Guid.NewGuid();
            }
            contactsDataGridView.Rows[rowIndexes[i]].Tag = contacts[i].Id;
        }

        // Conversiones a cliente (specs/003, sección 7): primero se escribe el
        // alta en clients.json y solo si funciona se guarda contacts.json sin
        // los convertidos; un fallo no toca nada y se muestra (constitución, 5).
        if (_pendingConversions.Count > 0)
        {
            try
            {
                List<Client> clientsOnDisk = _dataStore.LoadClients();
                clientsOnDisk.AddRange(_pendingConversions);
                _dataStore.SaveClients(clientsOnDisk);
            }
            catch (Exception ex)
            {
                Prompt(
                    "No se han podido dar de alta los clientes convertidos; no se ha guardado nada.\n\n" +
                    ex.Message,
                    "CRM1 — Error al guardar",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        try
        {
            _dataStore.SaveContacts(contacts);
        }
        catch (Exception ex)
        {
            // Un fallo de escritura nunca se presenta como éxito (constitución, punto 5).
            Prompt(
                "No se han podido guardar los futuros clientes.\n\n" + ex.Message,
                "CRM1 — Error al guardar",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _savedContacts = contacts;
        _dirty = false;

        int convertedCount = _pendingConversions.Count;
        _pendingConversions.Clear();
        _convertedContacts.Clear();

        // El futuro cliente actual pasa a ser el de la fila editada, ya con su
        // identificador asignado (skill «seleccionar-cliente», regla 4).
        if (_activeRow is not null && _activeRow.Tag is Guid savedId)
        {
            _currentContactId = savedId;
        }

        // Todo vuelve a solo lectura y las filas vacías se retiran de la vista.
        RemoveEmptyRows();
        EndEditingSession();
        EnsureCurrentContactExists();

        Prompt(
            convertedCount > 0
                ? $"Se han guardado {contacts.Count} futuro(s) cliente(s) y se ha(n) dado de alta {convertedCount} cliente(s) correctamente."
                : $"Se han guardado {contacts.Count} futuro(s) cliente(s) correctamente.",
            "CRM1 — Guardar",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>
    /// Retira de la vista las filas totalmente vacías (nunca se guardan).
    /// </summary>
    private void RemoveEmptyRows()
    {
        for (int i = contactsDataGridView.Rows.Count - 1; i >= 0; i--)
        {
            if (IsEmptyRow(contactsDataGridView.Rows[i]))
            {
                contactsDataGridView.Rows.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Indica si la tabla (filas no vacías) difiere de la última copia guardada,
    /// comparando por identificador de futuro cliente para no depender del orden.
    /// </summary>
    private bool GridDiffersFromSaved()
    {
        var current = new List<Contact>();
        for (int i = 0; i < contactsDataGridView.Rows.Count; i++)
        {
            System.Windows.Forms.DataGridViewRow row = contactsDataGridView.Rows[i];
            if (IsEmptyRow(row))
            {
                continue;
            }
            current.Add(MapRowToContact(row, out _));
        }

        if (current.Count != _savedContacts.Count)
        {
            return true;
        }

        var savedById = new Dictionary<Guid, Contact>();
        foreach (Contact contact in _savedContacts)
        {
            savedById[contact.Id] = contact;
        }

        foreach (Contact contact in current)
        {
            if (!savedById.TryGetValue(contact.Id, out Contact? saved) || !ContactsEqual(contact, saved))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Compara dos futuros clientes campo a campo.
    /// </summary>
    private static bool ContactsEqual(Contact a, Contact b)
    {
        return string.Equals(a.FirstName, b.FirstName, StringComparison.Ordinal)
            && string.Equals(a.LastName1, b.LastName1, StringComparison.Ordinal)
            && string.Equals(a.LastName2, b.LastName2, StringComparison.Ordinal)
            && string.Equals(a.TaxId, b.TaxId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(a.Address, b.Address, StringComparison.Ordinal)
            && string.Equals(a.PostalCode, b.PostalCode, StringComparison.Ordinal)
            && string.Equals(a.City, b.City, StringComparison.OrdinalIgnoreCase)
            && string.Equals(a.Province, b.Province, StringComparison.Ordinal)
            && string.Equals(a.Phone, b.Phone, StringComparison.Ordinal)
            && string.Equals(a.Email, b.Email, StringComparison.OrdinalIgnoreCase)
            && string.Equals(a.Notes, b.Notes, StringComparison.Ordinal)
            && a.RegistrationDate == b.RegistrationDate
            && a.Status == b.Status;
    }

    /// <summary>
    /// Convierte una fila en un futuro cliente; los errores de forma (fecha o
    /// estado) se devuelven como error de validación. La fecha puede estar
    /// vacía y el estado vacío se guarda como Pendiente (specs/003).
    /// </summary>
    private static Contact MapRowToContact(System.Windows.Forms.DataGridViewRow row, out ContactValidationError? error)
    {
        error = null;

        // Estado: desplegable con dos valores; vacío = Pendiente.
        string statusText = CellText(row, "statusColumn");
        ContactStatus status = statusText switch
        {
            "Descartado" => ContactStatus.Discarded,
            _ => ContactStatus.Pending
        };
        if (statusText is not ("Pendiente" or "Descartado" or ""))
        {
            error = new ContactValidationError
            {
                Field = nameof(Contact.Status),
                Message = "El estado no es válido."
            };
        }

        // Fecha de alta en formato dd/MM/aaaa; puede estar vacía.
        DateTime registrationDate = default;
        if (error is null)
        {
            string dateText = CellText(row, "registrationDateColumn");
            if (dateText.Length > 0
                && !DateTime.TryParseExact(dateText, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out registrationDate))
            {
                error = new ContactValidationError
                {
                    Field = nameof(Contact.RegistrationDate),
                    Message = "La fecha no es válida (dd/mm/aaaa)."
                };
            }
        }

        return new Contact
        {
            Id = row.Tag is Guid id ? id : Guid.Empty,
            FirstName = CellText(row, "firstNameColumn"),
            LastName1 = CellText(row, "lastName1Column"),
            LastName2 = CellText(row, "lastName2Column"),
            TaxId = CellText(row, "taxIdColumn"),
            Phone = CellText(row, "phoneColumn"),
            Email = CellText(row, "emailColumn"),
            Address = CellText(row, "addressColumn"),
            PostalCode = CellText(row, "postalCodeColumn"),
            City = CellText(row, "cityColumn"),
            Province = CellText(row, "provinceColumn"),
            Status = status,
            RegistrationDate = registrationDate,
            Notes = CellText(row, "notesColumn")
        };
    }

    /// <summary>
    /// Recorta los valores de la fila, normaliza documento y teléfono, corrige
    /// las mayúsculas según las normas de la lengua (inicial de cada palabra,
    /// partículas en minúscula; observaciones solo la primera letra) y deja el
    /// estado vacío como Pendiente, escribiendo el resultado en las celdas.
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

        row.Cells["taxIdColumn"].Value = SpanishDocuments.Normalize(CellText(row, "taxIdColumn"));
        row.Cells["phoneColumn"].Value = ClientService.NormalizePhone(CellText(row, "phoneColumn"));

        // El estado vacío se guarda como Pendiente (specs/003).
        if (CellText(row, "statusColumn").Length == 0)
        {
            row.Cells["statusColumn"].Value = DefaultStatusText;
        }

        // Corrección de mayúsculas (specs/003): si el usuario no pone la
        // inicial mayúscula, el programa la corrige al guardar.
        row.Cells["firstNameColumn"].Value = TextNormalizer.CapitalizeWords(CellText(row, "firstNameColumn"));
        row.Cells["lastName1Column"].Value = TextNormalizer.CapitalizeWords(CellText(row, "lastName1Column"));
        row.Cells["lastName2Column"].Value = TextNormalizer.CapitalizeWords(CellText(row, "lastName2Column"));
        row.Cells["addressColumn"].Value = TextNormalizer.CapitalizeWords(CellText(row, "addressColumn"));
        row.Cells["cityColumn"].Value = TextNormalizer.CapitalizeWords(CellText(row, "cityColumn"));
        row.Cells["notesColumn"].Value = TextNormalizer.CapitalizeSentence(CellText(row, "notesColumn"));
    }

    /// <summary>
    /// Indica si todas las celdas de la fila están vacías. El estado Pendiente
    /// con el que nace la fila nueva no cuenta como dato.
    /// </summary>
    private static bool IsEmptyRow(System.Windows.Forms.DataGridViewRow row)
    {
        foreach (System.Windows.Forms.DataGridViewCell cell in row.Cells)
        {
            if (string.IsNullOrWhiteSpace(cell.Value?.ToString()))
            {
                continue;
            }

            // Una fila con solo el estado por defecto sigue estando vacía.
            if (cell.OwningColumn.Name == "statusColumn"
                && cell.Value?.ToString() == DefaultStatusText)
            {
                continue;
            }

            return false;
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
    /// Muestra una línea por error, solo con el mensaje corto, y marca las celdas
    /// afectadas con un borde rojo (specs/003, sección 4.1).
    /// </summary>
    private void ShowValidationErrors(List<(int Row, ContactValidationError Error)> errors)
    {
        foreach (var (row, contactError) in errors)
        {
            string? columnName = ColumnNameFor(contactError.Field);
            if (columnName is not null)
            {
                _errorCells.Add(contactsDataGridView.Rows[row].Cells[columnName]);
            }
        }
        contactsDataGridView.Invalidate();

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
        contactsDataGridView.Invalidate();
    }

    /// <summary>
    /// Nombre de columna de la tabla para un campo del modelo.
    /// </summary>
    private static string? ColumnNameFor(string field) => field switch
    {
        nameof(Contact.FirstName) => "firstNameColumn",
        nameof(Contact.LastName1) => "lastName1Column",
        nameof(Contact.LastName2) => "lastName2Column",
        nameof(Contact.TaxId) => "taxIdColumn",
        nameof(Contact.Phone) => "phoneColumn",
        nameof(Contact.Email) => "emailColumn",
        nameof(Contact.Address) => "addressColumn",
        nameof(Contact.PostalCode) => "postalCodeColumn",
        nameof(Contact.City) => "cityColumn",
        nameof(Contact.Province) => "provinceColumn",
        nameof(Contact.Status) => "statusColumn",
        nameof(Contact.RegistrationDate) => "registrationDateColumn",
        _ => null
    };

    /// <summary>
    /// Marca que hay cambios sin guardar cuando cambia una celda.
    /// </summary>
    private void ContactsDataGridView_CellValueChanged(object? sender, System.Windows.Forms.DataGridViewCellEventArgs e)
    {
        if (!_loading && e.RowIndex >= 0)
        {
            _dirty = true;
        }

        // Al corregir una celda marcada, su borde rojo desaparece al momento.
        if (e.RowIndex >= 0 && e.ColumnIndex >= 0
            && _errorCells.Remove(contactsDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex]))
        {
            contactsDataGridView.Invalidate();
        }
    }

    /// <summary>
    /// Marca que hay cambios sin guardar cuando se añade una fila; toda fila nueva
    /// nace en solo lectura (la activa se desbloquea al activarse).
    /// </summary>
    private void ContactsDataGridView_RowsAdded(object? sender, System.Windows.Forms.DataGridViewRowsAddedEventArgs e)
    {
        if (!_loading)
        {
            _dirty = true;
        }

        if (e.RowIndex >= 0)
        {
            contactsDataGridView.Rows[e.RowIndex].ReadOnly = true;
        }
    }

    /// <summary>
    /// Dibuja el borde rojo alrededor de las celdas con errores de validación.
    /// Las celdas permanecen siempre blancas para escribir.
    /// </summary>
    private void ContactsDataGridView_CellPainting(object? sender, System.Windows.Forms.DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        System.Windows.Forms.DataGridViewCell cell = contactsDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex];
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
    /// Evita que la tabla lance excepciones con valores ausentes de los desplegables.
    /// </summary>
    private void ContactsDataGridView_DataError(object? sender, System.Windows.Forms.DataGridViewDataErrorEventArgs e)
    {
        e.ThrowException = false;
    }

    /// <summary>
    /// Marca como futuro cliente actual la fila seleccionada (clic en cualquier
    /// celda o teclado). Sin selección no hay futuro cliente actual, nunca un
    /// valor inventado (skill «seleccionar-cliente», reglas 1 y 2).
    /// </summary>
    private void ContactsDataGridView_SelectionChanged(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        System.Windows.Forms.DataGridViewRow? selected = contactsDataGridView.SelectedRows.Count > 0
            ? contactsDataGridView.SelectedRows[0]
            : null;

        // Durante la edición las demás filas están deshabilitadas: la selección
        // (clic o teclado) se devuelve a la fila en edición. Si esa fila ya se
        // retiró de la tabla (Cancelar sobre una fila nueva), no se toca: sus
        // celdas dejaron de pertenecer al control y reasignar la celda actual
        // lanzaría «La celda proporcionada no pertenece a este control».
        if (_activeRow is not null && _activeRow.Index >= 0
            && !_restoringSelection && selected != _activeRow)
        {
            _restoringSelection = true;
            try
            {
                contactsDataGridView.ClearSelection();

                // Devuelve también la celda actual a la fila en edición,
                // manteniendo la columna en la que estaba el usuario.
                int column = contactsDataGridView.CurrentCell?.ColumnIndex ?? 0;
                if (column >= 0 && column < _activeRow.Cells.Count)
                {
                    contactsDataGridView.CurrentCell = _activeRow.Cells[column];
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

        _currentContactId = selected?.Tag is Guid id ? id : null;
    }

    /// <summary>
    /// Dibuja el borde grueso alrededor de la fila seleccionada (futuro cliente
    /// actual, skill «seleccionar-cliente»): clic, teclado o fila en edición.
    /// Las celdas siguen blancas y el borde rojo de errores (más fino) queda dentro.
    /// </summary>
    private void ContactsDataGridView_RowPostPaint(object? sender, System.Windows.Forms.DataGridViewRowPostPaintEventArgs e)
    {
        if (e.RowIndex < 0 || !contactsDataGridView.Rows[e.RowIndex].Selected)
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
    /// Busca la fila del futuro cliente con el identificador indicado (rastreo
    /// por Id, nunca por índice: admite ordenar o recargar la tabla).
    /// </summary>
    private System.Windows.Forms.DataGridViewRow? FindRowById(Guid? contactId)
    {
        if (contactId is null)
        {
            return null;
        }

        foreach (System.Windows.Forms.DataGridViewRow row in contactsDataGridView.Rows)
        {
            if (row.Tag is Guid id && id == contactId)
            {
                return row;
            }
        }

        return null;
    }

    /// <summary>
    /// Limpia la marca de futuro cliente actual si su fila ya no existe (regla 5
    /// de la skill «seleccionar-cliente»: sin identificadores huérfanos).
    /// </summary>
    private void EnsureCurrentContactExists()
    {
        if (_currentContactId is not null && FindRowById(_currentContactId) is null)
        {
            _currentContactId = null;
        }
    }
}
