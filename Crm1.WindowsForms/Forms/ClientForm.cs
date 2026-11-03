using System.Globalization;
using Crm1.WindowsForms.Models;
using Crm1.WindowsForms.Services;

namespace Crm1.WindowsForms.Forms;

/// <summary>
/// Formulario del módulo de clientes (specs/002-formulario-clientes.md).
/// La tabla es de solo lectura por defecto: Nuevo y Modificar activan una fila
/// para editarla y Guardar la persiste (patrón de la skill «modificar»).
/// La lógica de negocio vive en Services (constitución, punto 3).
/// </summary>
public partial class ClientForm : System.Windows.Forms.Form
{
    // Color del borde que marca las celdas con errores de validación.
    private static readonly System.Drawing.Color ErrorBorderColor = System.Drawing.Color.FromArgb(220, 38, 38);

    // Color y grosor del borde que marca la fila seleccionada (cliente actual,
    // skill «seleccionar-cliente»); más grueso que el de errores para distinguirlo.
    private static readonly System.Drawing.Color SelectionBorderColor = System.Drawing.Color.FromArgb(100, 116, 139);
    private const float SelectionBorderWidth = 3F;

    // Color del texto de las filas deshabilitadas mientras hay edición abierta.
    private static readonly System.Drawing.Color DisabledRowTextColor = System.Drawing.Color.FromArgb(156, 163, 175);

    private readonly IDataStore _dataStore;
    private readonly PostalCodeCatalog? _catalog;
    private readonly ClientService? _clientService;

    // Falso si el catálogo de CP o el archivo de clientes no se pudieron cargar:
    // todos los botones de gestión quedan inactivos (constitución, punto 5).
    private bool _canSave;

    // Fila activa para edición (rastreada por referencia); null si no hay sesión.
    private System.Windows.Forms.DataGridViewRow? _activeRow;

    // Instantánea de los valores de la fila activa al empezar la edición,
    // para restaurarlos si se pulsa Cancelar.
    private List<object?> _activeRowSnapshot = new();

    // Última copia guardada de los clientes, para recalcula si quedan cambios.
    private List<Client> _savedClients = new();

    // Celdas con errores de validación: reciben un borde rojo alrededor.
    private readonly HashSet<System.Windows.Forms.DataGridViewCell> _errorCells = new();

    // Verdadero mientras la tabla se rellena desde disco (no son cambios del usuario).
    private bool _loading;

    // Guarda anti-reentrada al devolver la selección a la fila en edición.
    private bool _restoringSelection;

    // Verdadero cuando hay cambios sin guardar.
    private bool _dirty;

    /// <summary>
    /// Verdadero cuando la tabla tiene cambios sin guardar. Lo consulta MainForm
    /// antes de cerrar la aplicación porque ClientForm va embebido (specs/002).
    /// </summary>
    public bool HasUnsavedChanges => _dirty;

    // Cliente actual marcado por el usuario (skill «seleccionar-cliente»):
    // rastreo por identificador, nunca por índice.
    private Guid? _currentClientId;

    /// <summary>
    /// Identificador del cliente actual (fila marcada con un clic en cualquier
    /// celda), o null si no hay selección. Skill «seleccionar-cliente».
    /// </summary>
    public Guid? CurrentClientId => _currentClientId;

    /// <summary>
    /// Cliente actual reconstruido de la fila marcada, o null si no hay marca.
    /// </summary>
    public Client? CurrentClient => FindRowById(_currentClientId) is { } row
        ? MapRowToClient(row, out _)
        : null;

    /// <summary>
    /// Verdadero mientras hay una fila en edición (Nuevo o Modificar). Lo
    /// consulta la búsqueda global antes de navegar hasta un cliente.
    /// </summary>
    public bool IsEditing => _activeRow is not null;

    /// <summary>
    /// Marca como cliente actual el cliente con el identificador indicado y
    /// desplaza la tabla hasta su fila (búsqueda global, specs/004). Devuelve
    /// false si no está en la tabla o hay una edición en curso.
    /// </summary>
    public bool SelectClientById(Guid clientId)
    {
        if (_activeRow is not null)
        {
            return false;
        }

        System.Windows.Forms.DataGridViewRow? row = FindRowById(clientId);
        if (row is null)
        {
            return false;
        }

        clientsDataGridView.CurrentCell = row.Cells["firstNameColumn"];
        clientsDataGridView.FirstDisplayedScrollingRowIndex = row.Index;
        return true;
    }

    /// <summary>
    /// Crea el formulario, carga el catálogo de códigos postales y los clientes
    /// guardados. Si algo falla, todos los botones de gestión quedan inactivos
    /// para no sobrescribir datos (constitución, punto 5).
    /// </summary>
    public ClientForm()
    {
        InitializeComponent();

        PostalCodeCatalog? catalog = null;
        try
        {
            string catalogPath = Path.Combine(AppContext.BaseDirectory, "Data", "postal-codes-es.json");
            catalog = new PostalCodeCatalog(catalogPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "No se pudo cargar el archivo de códigos postales (Data/postal-codes-es.json).\n\n" +
                "Los botones de gestión se han desactivado porque no se pueden validar los datos.\n\n" +
                ex.Message,
                "CRM1 — Error de datos",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        _catalog = catalog;
        _clientService = catalog is null ? null : new ClientService(catalog);
        _canSave = _clientService is not null;

        _dataStore = new JsonDataStore();

        if (_catalog is not null)
        {
            PopulateProvinceColumn();
        }

        LoadClients();
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
    /// validar y guardar datos. Eliminar está pendiente: visible pero inactivo.
    /// </summary>
    private void UpdateButtonStates()
    {
        bool editing = _activeRow is not null;

        // Eliminar solo con el formulario en estado normal (skill «borrar»).
        deleteButton.Enabled = _canSave && !editing;
        newButton.Enabled = _canSave && !editing;
        modifyButton.Enabled = _canSave && !editing;
        saveButton.Enabled = _canSave;
        cancelButton.Enabled = _canSave && editing;
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

        // Durante la edición, el cliente actual es el de esta fila (skill
        // «seleccionar-cliente», regla 6).
        if (row.Tag is Guid activeId)
        {
            _currentClientId = activeId;
        }

        // Las demás filas quedan deshabilitadas: atenuadas en gris y sin
        // selección posible mientras dure la edición (specs/002).
        foreach (System.Windows.Forms.DataGridViewRow other in clientsDataGridView.Rows)
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
        foreach (System.Windows.Forms.DataGridViewRow gridRow in clientsDataGridView.Rows)
        {
            gridRow.DefaultCellStyle.ForeColor = System.Drawing.SystemColors.ControlText;
        }
        SetColumnsSortable(true);
        UpdateButtonStates();
    }

    /// <summary>
    /// Activa o desactiva la ordenación por cabeceras (bloqueada durante la edición
    /// para no descuadrar la fila abierta). Las columnas de desplegable nunca ordenan.
    /// </summary>
    private void SetColumnsSortable(bool sortable)
    {
        foreach (System.Windows.Forms.DataGridViewColumn column in clientsDataGridView.Columns)
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
    /// Carga en la tabla los clientes guardados en disco. Si el archivo está dañado,
    /// todos los botones de gestión quedan inactivos para no sobrescribir los datos.
    /// </summary>
    private void LoadClients()
    {
        List<Client> clients;
        try
        {
            clients = _dataStore.LoadClients();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "No se pudo leer el archivo de clientes (Data/clients.json).\n\n" +
                "Los botones de gestión se han desactivado para no sobrescribir los datos.\n\n" +
                ex.Message,
                "CRM1 — Error de datos",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _canSave = false;
            return;
        }

        _loading = true;
        foreach (Client client in clients)
        {
            AddClientRow(client);
        }
        _loading = false;
        _savedClients = clients;
        _dirty = false;

        // Sin cliente actual hasta que el usuario marque una fila con un clic
        // (skill «seleccionar-cliente»).
        _currentClientId = null;
        clientsDataGridView.ClearSelection();
    }

    /// <summary>
    /// Añade a la tabla una fila con los datos de un cliente guardado.
    /// </summary>
    private void AddClientRow(Client client)
    {
        // Si la provincia guardada no está en la lista oficial se añade para poder
        // mostrarla; la validación de Guardar la detectará si no es válida.
        string province = client.Province;
        if (!string.IsNullOrWhiteSpace(province) && !provinceColumn.Items.Contains(province))
        {
            provinceColumn.Items.Add(province);
        }

        int index = clientsDataGridView.Rows.Add(
            client.FirstName,
            client.LastName1,
            client.LastName2,
            client.TaxId,
            client.Phone,
            client.Email,
            client.Address,
            client.PostalCode,
            client.City,
            province,
            client.Status == ClientStatus.Inactive ? "Inactivo" : "Activo",
            client.RegistrationDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            client.Notes);

        // El identificador del cliente viaja en la fila sin mostrarse.
        clientsDataGridView.Rows[index].Tag = client.Id;
    }

    /// <summary>
    /// Inserta una fila vacía como primera fila, la activa para edición y comienza
    /// a editar el nombre.
    /// </summary>
    private void NewButton_Click(object? sender, EventArgs e)
    {
        if (!_canSave || _activeRow is not null)
        {
            return;
        }

        clientsDataGridView.Rows.Insert(0, 1);
        ActivateRowEditing(clientsDataGridView.Rows[0]);

        // Selecciona la fila nueva y entra a editar la celda Nombre.
        clientsDataGridView.CurrentCell = clientsDataGridView.Rows[0].Cells["firstNameColumn"];
        clientsDataGridView.BeginEdit(true);
    }

    /// <summary>
    /// Activa las celdas del cliente seleccionado para reescribir las que se quieran.
    /// </summary>
    private void ModifyButton_Click(object? sender, EventArgs e)
    {
        if (!_canSave || _activeRow is not null)
        {
            return;
        }

        // Cliente actual marcado con un clic en cualquier celda de su fila
        // (skill «seleccionar-cliente»): la fila se localiza por identificador.
        System.Windows.Forms.DataGridViewRow? row = FindRowById(_currentClientId);
        if (row is null)
        {
            MessageBox.Show(this,
                "Selecciona un cliente de la tabla.",
                "CRM1 — Modificar",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ActivateRowEditing(row);

        // Coloca el cursor en la primera celda de la fila.
        clientsDataGridView.CurrentCell = row.Cells["firstNameColumn"];
        clientsDataGridView.BeginEdit(true);
    }

    /// <summary>
    /// Elimina el cliente actual de la tabla tras pedir confirmación con su
    /// nombre. La eliminación es en memoria: solo se hace permanente al pulsar
    /// Guardar (skill «borrar»; constitución, punto 5).
    /// </summary>
    private void DeleteButton_Click(object? sender, EventArgs e)
    {
        if (!_canSave || _activeRow is not null)
        {
            return;
        }

        // Cliente actual marcado con un clic (skill «seleccionar-cliente»).
        System.Windows.Forms.DataGridViewRow? row = FindRowById(_currentClientId);
        if (row is null)
        {
            MessageBox.Show(this,
                "Selecciona un cliente de la tabla.",
                "CRM1 — Eliminar",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Confirmación antes de eliminar, identificando al cliente por su nombre.
        DialogResult result = MessageBox.Show(this,
            $"¿Seguro que quieres eliminar a {RowDisplayName(row)}?\n\n" +
            "Se quitará de la tabla y se eliminará por completo del archivo al pulsar Guardar.",
            "CRM1 — Eliminar",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        // Eliminación en memoria: solo Guardar escribe en disco.
        clientsDataGridView.Rows.Remove(row);

        // Si la fila eliminada era la marcada, la marca se limpia (regla 5).
        EnsureCurrentClientExists();

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

        return name.Length > 0 ? name : "este cliente";
    }

    /// <summary>
    /// Descarta la edición en curso: una fila nueva se elimina de la tabla y una
    /// modificada vuelve a los valores de la instantánea tomada al activarla.
    /// </summary>
    private void CancelButton_Click(object? sender, EventArgs e)
    {
        if (_activeRow is null)
        {
            return;
        }

        // Confirma la celda en edición antes de restaurar.
        clientsDataGridView.EndEdit();

        System.Windows.Forms.DataGridViewRow row = _activeRow;

        if (row.Tag is not Guid)
        {
            // Fila creada con Nuevo y aún no guardada: se elimina.
            clientsDataGridView.Rows.Remove(row);
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
        EnsureCurrentClientExists();

        EndEditingSession();
        ClearErrorMarks();

        // Recalcula si quedan otros cambios sin guardar (comparación con la última
        // copia guardada) para que el aviso al cerrar nunca mienta.
        _dirty = GridDiffersFromSaved();
    }

    /// <summary>
    /// Valida toda la tabla y, si todo es correcto, guarda los clientes en disco.
    /// </summary>
    private void SaveButton_Click(object? sender, EventArgs e)
    {
        if (!_canSave)
        {
            return;
        }

        // Confirma la celda en edición para leer su último valor.
        clientsDataGridView.EndEdit();

        // Limpia las marcas rojas de una validación anterior.
        ClearErrorMarks();

        var errors = new List<(int Row, ClientValidationError Error)>();
        var clients = new List<Client>();
        var rowIndexes = new List<int>();

        for (int i = 0; i < clientsDataGridView.Rows.Count; i++)
        {
            System.Windows.Forms.DataGridViewRow row = clientsDataGridView.Rows[i];

            // Las filas totalmente vacías se ignoran (p. ej. un Nuevo por accidente).
            if (IsEmptyRow(row))
            {
                continue;
            }

            // Normaliza lo escrito y lo refleja en las celdas, para que se guarde
            // y se muestre exactamente lo validado.
            NormalizeRowCells(row);

            Client client = MapRowToClient(row, out ClientValidationError? error);
            if (error is not null)
            {
                errors.Add((i, error!));
                continue;
            }

            clients.Add(client);
            rowIndexes.Add(i);
        }

        // Reglas de negocio fuera del formulario (constitución, punto 3).
        foreach (RowValidationError rowError in _clientService!.ValidateClients(clients))
        {
            errors.Add((rowIndexes[rowError.RowIndex], rowError.Error));
        }

        if (errors.Count > 0)
        {
            ShowValidationErrors(errors);
            return;
        }

        // Asigna identificadores a las filas que aún no tienen cliente guardado.
        for (int i = 0; i < clients.Count; i++)
        {
            if (clients[i].Id == Guid.Empty)
            {
                clients[i].Id = Guid.NewGuid();
            }
            clientsDataGridView.Rows[rowIndexes[i]].Tag = clients[i].Id;
        }

        try
        {
            _dataStore.SaveClients(clients);
        }
        catch (Exception ex)
        {
            // Un fallo de escritura nunca se presenta como éxito (constitución, punto 5).
            MessageBox.Show(this,
                "No se han podido guardar los clientes.\n\n" + ex.Message,
                "CRM1 — Error al guardar",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _savedClients = clients;
        _dirty = false;

        // El cliente actual pasa a ser el de la fila editada, ya con su
        // identificador asignado (skill «seleccionar-cliente», regla 4).
        if (_activeRow is not null && _activeRow.Tag is Guid savedId)
        {
            _currentClientId = savedId;
        }

        // Todo vuelve a solo lectura y las filas vacías se retiran de la vista.
        RemoveEmptyRows();
        EndEditingSession();
        EnsureCurrentClientExists();

        MessageBox.Show(this,
            $"Se han guardado {clients.Count} cliente(s) correctamente.",
            "CRM1 — Guardar",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>
    /// Retira de la vista las filas totalmente vacías (nunca se guardan).
    /// </summary>
    private void RemoveEmptyRows()
    {
        for (int i = clientsDataGridView.Rows.Count - 1; i >= 0; i--)
        {
            if (IsEmptyRow(clientsDataGridView.Rows[i]))
            {
                clientsDataGridView.Rows.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Indica si la tabla (filas no vacías) difiere de la última copia guardada,
    /// comparando por identificador de cliente para no depender del orden.
    /// </summary>
    private bool GridDiffersFromSaved()
    {
        var current = new List<Client>();
        for (int i = 0; i < clientsDataGridView.Rows.Count; i++)
        {
            System.Windows.Forms.DataGridViewRow row = clientsDataGridView.Rows[i];
            if (IsEmptyRow(row))
            {
                continue;
            }
            current.Add(MapRowToClient(row, out _));
        }

        if (current.Count != _savedClients.Count)
        {
            return true;
        }

        var savedById = new Dictionary<Guid, Client>();
        foreach (Client client in _savedClients)
        {
            savedById[client.Id] = client;
        }

        foreach (Client client in current)
        {
            if (!savedById.TryGetValue(client.Id, out Client? saved) || !ClientsEqual(client, saved))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Compara dos clientes campo a campo.
    /// </summary>
    private static bool ClientsEqual(Client a, Client b)
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
    /// Convierte una fila en un cliente; los errores de forma (fecha o estado) se
    /// devuelven como error de validación.
    /// </summary>
    private static Client MapRowToClient(System.Windows.Forms.DataGridViewRow row, out ClientValidationError? error)
    {
        error = null;

        // Estado: desplegable con dos valores.
        string statusText = CellText(row, "statusColumn");
        ClientStatus status = statusText switch
        {
            "Inactivo" => ClientStatus.Inactive,
            "Activo" => ClientStatus.Active,
            _ => ClientStatus.Active
        };
        if (statusText is not ("Activo" or "Inactivo"))
        {
            error = new ClientValidationError
            {
                Field = nameof(Client.Status),
                Message = "El estado no es válido."
            };
        }

        // Fecha de alta en formato dd/MM/aaaa.
        DateTime registrationDate = default;
        if (error is null)
        {
            string dateText = CellText(row, "registrationDateColumn");
            if (dateText.Length == 0)
            {
                error = new ClientValidationError
                {
                    Field = nameof(Client.RegistrationDate),
                    Message = "La celda está vacía."
                };
            }
            else if (!DateTime.TryParseExact(dateText, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out registrationDate))
            {
                error = new ClientValidationError
                {
                    Field = nameof(Client.RegistrationDate),
                    Message = "La fecha no es válida (dd/mm/aaaa)."
                };
            }
        }

        return new Client
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
    /// Recorta los valores de la fila y normaliza documento y teléfono, escribiendo
    /// el resultado en las propias celdas.
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
    }

    /// <summary>
    /// Indica si todas las celdas de la fila están vacías.
    /// </summary>
    private static bool IsEmptyRow(System.Windows.Forms.DataGridViewRow row)
    {
        foreach (System.Windows.Forms.DataGridViewCell cell in row.Cells)
        {
            if (!string.IsNullOrWhiteSpace(cell.Value?.ToString()))
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
    /// Muestra una línea por error, solo con el mensaje corto, y marca las celdas
    /// afectadas con un borde rojo (specs/002, sección 4.1).
    /// </summary>
    private void ShowValidationErrors(List<(int Row, ClientValidationError Error)> errors)
    {
        foreach (var (row, clientError) in errors)
        {
            string? columnName = ColumnNameFor(clientError.Field);
            if (columnName is not null)
            {
                _errorCells.Add(clientsDataGridView.Rows[row].Cells[columnName]);
            }
        }
        clientsDataGridView.Invalidate();

        IEnumerable<string> messages = errors.Select(item => item.Error.Message);
        MessageBox.Show(this,
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
        clientsDataGridView.Invalidate();
    }

    /// <summary>
    /// Nombre de columna de la tabla para un campo del modelo.
    /// </summary>
    private static string? ColumnNameFor(string field) => field switch
    {
        nameof(Client.FirstName) => "firstNameColumn",
        nameof(Client.LastName1) => "lastName1Column",
        nameof(Client.LastName2) => "lastName2Column",
        nameof(Client.TaxId) => "taxIdColumn",
        nameof(Client.Phone) => "phoneColumn",
        nameof(Client.Email) => "emailColumn",
        nameof(Client.Address) => "addressColumn",
        nameof(Client.PostalCode) => "postalCodeColumn",
        nameof(Client.City) => "cityColumn",
        nameof(Client.Province) => "provinceColumn",
        nameof(Client.Status) => "statusColumn",
        nameof(Client.RegistrationDate) => "registrationDateColumn",
        _ => null
    };

    /// <summary>
    /// Marca que hay cambios sin guardar cuando cambia una celda.
    /// </summary>
    private void ClientsDataGridView_CellValueChanged(object? sender, System.Windows.Forms.DataGridViewCellEventArgs e)
    {
        if (!_loading && e.RowIndex >= 0)
        {
            _dirty = true;
        }

        // Al corregir una celda marcada, su borde rojo desaparece al momento.
        if (e.RowIndex >= 0 && e.ColumnIndex >= 0
            && _errorCells.Remove(clientsDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex]))
        {
            clientsDataGridView.Invalidate();
        }
    }

    /// <summary>
    /// Marca que hay cambios sin guardar cuando se añade una fila; toda fila nueva
    /// nace en solo lectura (la activa se desbloquea al activarse).
    /// </summary>
    private void ClientsDataGridView_RowsAdded(object? sender, System.Windows.Forms.DataGridViewRowsAddedEventArgs e)
    {
        if (!_loading)
        {
            _dirty = true;
        }

        if (e.RowIndex >= 0)
        {
            clientsDataGridView.Rows[e.RowIndex].ReadOnly = true;
        }
    }

    /// <summary>
    /// Dibuja el borde rojo alrededor de las celdas con errores de validación.
    /// Las celdas permanecen siempre blancas para escribir.
    /// </summary>
    private void ClientsDataGridView_CellPainting(object? sender, System.Windows.Forms.DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        System.Windows.Forms.DataGridViewCell cell = clientsDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex];
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
    private void ClientsDataGridView_DataError(object? sender, System.Windows.Forms.DataGridViewDataErrorEventArgs e)
    {
        e.ThrowException = false;
    }

    /// <summary>
    /// Marca como cliente actual la fila seleccionada (clic en cualquier celda o
    /// teclado). Sin selección no hay cliente actual, nunca un valor inventado
    /// (skill «seleccionar-cliente», reglas 1 y 2).
    /// </summary>
    private void ClientsDataGridView_SelectionChanged(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        System.Windows.Forms.DataGridViewRow? selected = clientsDataGridView.SelectedRows.Count > 0
            ? clientsDataGridView.SelectedRows[0]
            : null;

        // Durante la edición las demás filas están deshabilitadas: la selección
        // (clic o teclado) se devuelve a la fila en edición.
        if (_activeRow is not null && !_restoringSelection && selected != _activeRow)
        {
            _restoringSelection = true;
            try
            {
                clientsDataGridView.ClearSelection();

                // Devuelve también la celda actual a la fila en edición,
                // manteniendo la columna en la que estaba el usuario.
                int column = clientsDataGridView.CurrentCell?.ColumnIndex ?? 0;
                if (column >= 0 && column < _activeRow.Cells.Count)
                {
                    clientsDataGridView.CurrentCell = _activeRow.Cells[column];
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

        _currentClientId = selected?.Tag is Guid id ? id : null;
    }

    /// <summary>
    /// Dibuja el borde grueso alrededor de la fila seleccionada (cliente actual,
    /// skill «seleccionar-cliente»): clic, teclado o fila en edición. Las celdas
    /// siguen blancas y el borde rojo de errores (más fino) queda dentro.
    /// </summary>
    private void ClientsDataGridView_RowPostPaint(object? sender, System.Windows.Forms.DataGridViewRowPostPaintEventArgs e)
    {
        if (e.RowIndex < 0 || !clientsDataGridView.Rows[e.RowIndex].Selected)
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
    /// Busca la fila del cliente con el identificador indicado (rastreo por Id,
    /// nunca por índice: admite ordenar o recargar la tabla).
    /// </summary>
    private System.Windows.Forms.DataGridViewRow? FindRowById(Guid? clientId)
    {
        if (clientId is null)
        {
            return null;
        }

        foreach (System.Windows.Forms.DataGridViewRow row in clientsDataGridView.Rows)
        {
            if (row.Tag is Guid id && id == clientId)
            {
                return row;
            }
        }

        return null;
    }

    /// <summary>
    /// Limpia la marca de cliente actual si su fila ya no existe (regla 5 de la
    /// skill «seleccionar-cliente»: sin identificadores huérfanos).
    /// </summary>
    private void EnsureCurrentClientExists()
    {
        if (_currentClientId is not null && FindRowById(_currentClientId) is null)
        {
            _currentClientId = null;
        }
    }
}
