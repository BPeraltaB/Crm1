using System.Globalization;
using Crm1.WindowsForms.Models;
using Crm1.WindowsForms.Services;

namespace Crm1.WindowsForms.Forms;

/// <summary>
/// Formulario del módulo de pedidos y abonos (specs/009): registro de la
/// actividad comercial de los clientes —el documento vive en el programa de
/// administración— con totales por año y por cliente. La tabla es de solo
/// lectura por defecto: Nuevo activa una fila para editarla y Guardar la
/// persiste (skill «modificar»). La lógica de negocio vive en OrderService
/// (constitución, punto 3).
/// </summary>
public partial class OrderForm : System.Windows.Forms.Form
{
    // Color del borde que marca las celdas con errores de validación.
    private static readonly System.Drawing.Color ErrorBorderColor = System.Drawing.Color.FromArgb(220, 38, 38);

    // Color y grosor del borde que marca la fila seleccionada, igual que en
    // los demás módulos (skill «seleccionar-cliente»).
    private static readonly System.Drawing.Color SelectionBorderColor = System.Drawing.Color.FromArgb(100, 116, 139);
    private const float SelectionBorderWidth = 3F;

    // Color del texto de las filas deshabilitadas mientras hay edición abierta.
    private static readonly System.Drawing.Color DisabledRowTextColor = System.Drawing.Color.FromArgb(156, 163, 175);

    // Texto por defecto con el que nace cada fila nueva (specs/009 §3).
    private const string DefaultKindText = "Pedido";

    // Almacén SQL con los pedidos (LoadOrders/SaveOrders, specs/009 y specs/006).
    private readonly SqlDataStore? _dataStore;

    // Reglas de negocio de los pedidos; null si la base no se pudo leer.
    private OrderService? _orderService;

    // Falso si la base de datos no se pudo abrir o leer: todos los botones
    // de gestión quedan inactivos (constitución, punto 5).
    private bool _canSave;

    // Fila activa para edición (rastreada por referencia); null si no hay sesión.
    private System.Windows.Forms.DataGridViewRow? _activeRow;

    // Instantánea de los valores de la fila activa al empezar la edición,
    // para restaurarlos si se pulsa Cancelar.
    private List<object?> _activeRowSnapshot = new();

    // Última copia guardada de los movimientos visibles, para recalcular cambios.
    private List<Order> _savedOrders = new();

    // Celdas con errores de validación: reciben un borde rojo alrededor.
    private readonly HashSet<System.Windows.Forms.DataGridViewCell> _errorCells = new();

    // Verdadero mientras la tabla se rellena desde disco (no son cambios del usuario).
    private bool _loading;

    // Guarda anti-reentrada al devolver la selección a la fila en edición.
    private bool _restoringSelection;

    // Verdadero cuando hay cambios sin guardar.
    private bool _dirty;

    // Ruta y sello temporal de la base de datos: detecta movimientos escritos
    // desde fuera para recargar al volver a mostrarse (mismo criterio que
    // ClientForm).
    private readonly string _databaseFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "crm1.db");
    private DateTime _lastDatabaseStamp;

    // Mapas del desplegable «Cliente»: texto visible ↔ cliente (solo clientes:
    // un futuro cliente con pedido ya sería cliente, specs/009).
    private readonly Dictionary<string, Guid> _clientByText = new();
    private readonly Dictionary<Guid, string> _clientTextById = new();

    // Movimiento actual marcado por el usuario (skill «seleccionar-cliente»):
    // rastreo por identificador, nunca por índice.
    private Guid? _currentOrderId;

    // Filtro activo por cliente (botón Pedidos de ClientForm, specs/009 §7);
    // null = sin filtro, se ven todos los movimientos.
    private Guid? _filterClientId;
    private string _filterText = string.Empty;

    // Movimientos ocultos por el filtro: se conservan en memoria y se guardan
    // junto con los visibles, así el filtro nunca hace perder datos.
    private List<Order> _hiddenOrders = new();

    /// <summary>
    /// Identificador del movimiento actual (fila marcada con un clic), o null
    /// si no hay selección. Skill «seleccionar-cliente».
    /// </summary>
    public Guid? CurrentOrderId => _currentOrderId;

    /// <summary>
    /// Pedido o abono actual reconstruido de la fila marcada, o null si no
    /// hay marca.
    /// </summary>
    public Order? CurrentOrder => FindRowById(_currentOrderId) is { } row
        ? MapRowToOrder(row, out _)
        : null;

    /// <summary>
    /// Verdadero cuando la tabla tiene cambios sin guardar. Lo consulta MainForm
    /// antes de cerrar la aplicación porque OrderForm va embebido (specs/009 §7).
    /// </summary>
    public bool HasUnsavedChanges => _dirty;

    /// <summary>
    /// Verdadero mientras hay una fila en edición (Nuevo o Modificar). Lo usa
    /// la búsqueda global antes de navegar hasta un pedido (specs/004).
    /// </summary>
    public bool IsEditing => _activeRow is not null;

    /// <summary>
    /// Marca como movimiento actual el del identificador indicado y desplaza
    /// la tabla hasta su fila (búsqueda global, specs/004). Devuelve false si
    /// hay una edición en curso o el pedido no está en la vista actual.
    /// </summary>
    public bool SelectOrderById(Guid orderId)
    {
        if (_activeRow is not null)
        {
            return false;
        }

        System.Windows.Forms.DataGridViewRow? row = FindRowById(orderId);
        if (row is null)
        {
            return false;
        }

        ordersDataGridView.CurrentCell = row.Cells["kindColumn"];
        ordersDataGridView.FirstDisplayedScrollingRowIndex = row.Index;
        return true;
    }

    /// <summary>
    /// Muestra solo los movimientos del cliente indicado, con el aviso
    /// «Pedidos de: …» y el botón «Ver todos» (botón Pedidos de ClientForm,
    /// specs/009 §7). Con edición o cambios pendientes no se cambia de vista:
    /// primero se guarda o se cancela (constitución, punto 5). Guardar
    /// conserva los movimientos ocultos por el filtro.
    /// </summary>
    public void ShowOrdersOf(Guid clientId, string displayName)
    {
        if (_activeRow is not null || _dirty)
        {
            return;
        }

        _filterClientId = clientId;
        _filterText = $"(Cliente) {displayName}";
        filterLabel.Text = $"Pedidos de: {_filterText}";
        filterLabel.Visible = true;
        showAllButton.Visible = true;
        LoadOrders();
    }

    /// <summary>
    /// Quita el filtro y vuelve a mostrar todos los movimientos (botón «Ver
    /// todos» o item «Pedidos» del menú).
    /// </summary>
    public void ShowAllOrders()
    {
        if (_activeRow is not null || _dirty)
        {
            return;
        }

        _filterClientId = null;
        _filterText = string.Empty;
        filterLabel.Visible = false;
        showAllButton.Visible = false;
        LoadOrders();
    }

    /// <summary>
    /// Atiende el botón «Ver todos»: quita el filtro del listado.
    /// </summary>
    private void ShowAllButton_Click(object? sender, EventArgs e) => ShowAllOrders();

    /// <summary>
    /// Diálogos del formulario (avisos, confirmaciones y errores): todo pasa
    /// por aquí. En la aplicación es un MessageBox con este formulario como
    /// dueño; las pruebas automatizadas de interfaz lo sustituyen por un
    /// delegado que responde sin bloquear.
    /// </summary>
    internal Func<string, string, MessageBoxButtons, MessageBoxIcon, DialogResult> Prompt { get; set; }

    /// <summary>
    /// Crea el formulario y carga los movimientos guardados. Si la base de
    /// datos falla, la tabla queda vacía y los botones inactivos
    /// (constitución, punto 5).
    /// </summary>
    public OrderForm() : this(null)
    {
    }

    /// <summary>
    /// Constructor con costura de pruebas: permite sustituir los diálogos
    /// antes de que la constructora cargue los datos, porque un error de base
    /// de datos durante la carga ya muestra un mensaje.
    /// </summary>
    internal OrderForm(Func<string, string, MessageBoxButtons, MessageBoxIcon, DialogResult>? prompt)
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

        LoadOrders();
    }

    /// <summary>
    /// Carga en la tabla los movimientos guardados y prepara el desplegable
    /// de clientes. La vista queda ordenada por fecha descendente (lo más
    /// reciente arriba, specs/009 §3).
    /// </summary>
    private void LoadOrders()
    {
        List<Order> orders;
        List<Client> clients;
        try
        {
            orders = _dataStore!.LoadOrders();
            clients = _dataStore.LoadClients();
        }
        catch (Exception ex)
        {
            ReportDataError(ex);
            return;
        }

        _loading = true;
        ordersDataGridView.Rows.Clear();
        PopulateClientColumn(clients);

        // Con el filtro activo solo se muestran los movimientos del cliente;
        // los demás quedan ocultos y se conservan para Guardar.
        List<Order> visible = orders;
        if (_filterClientId is Guid filterClientId)
        {
            visible = orders.Where(order => order.ClientId == filterClientId).ToList();
            _hiddenOrders = orders.Where(order => order.ClientId != filterClientId).ToList();
        }
        else
        {
            _hiddenOrders.Clear();
        }

        foreach (Order order in visible.OrderByDescending(order => order.OrderDate))
        {
            AddOrderRow(order);
        }

        _loading = false;
        _savedOrders = visible;
        _dirty = false;
        _canSave = true;
        _lastDatabaseStamp = GetDatabaseStamp();

        // Sin movimiento actual hasta que el usuario marque una fila con un
        // clic (skill «seleccionar-cliente»).
        _currentOrderId = null;
        ordersDataGridView.ClearSelection();
        PopulateYearFilter(visible);
        ApplyFilters();
        UpdateButtonStates();
    }

    /// <summary>
    /// Rellena el desplegable «Cliente» solo con clientes, y construye los
    /// mapas texto ↔ cliente que usa Guardar para reconstruir el vínculo.
    /// Si dos clientes se llaman igual, el segundo añade « (2)».
    /// </summary>
    private void PopulateClientColumn(List<Client> clients)
    {
        clientColumn.Items.Clear();
        _clientByText.Clear();
        _clientTextById.Clear();

        foreach (Client client in clients)
        {
            AddClientItem(
                $"(Cliente) {DisplayName(client.FirstName, client.LastName1, client.LastName2)}",
                client.Id);
        }

        // Las reglas de negocio necesitan saber qué clientes existen (solo
        // clientes: los movimientos nunca son de futuros, specs/009).
        var relatedRecords = clients.ToDictionary(client => client.Id, _ => TaskRelatedType.Client);
        _orderService = new OrderService(relatedRecords);
    }

    // Añade una entrada al desplegable con un texto único.
    private void AddClientItem(string baseText, Guid id)
    {
        string text = baseText;
        for (int attempt = 2; _clientByText.ContainsKey(text); attempt++)
        {
            text = $"{baseText} ({attempt})";
        }

        _clientByText[text] = id;
        _clientTextById[id] = text;
        clientColumn.Items.Add(text);
    }

    // Nombre para mostrar: nombre y apellidos no vacíos (razón social en
    // empresas).
    private static string DisplayName(string firstName, string lastName1, string lastName2) =>
        string.Join(" ", new[] { firstName, lastName1, lastName2 }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

    /// <summary>
    /// Añade a la tabla una fila con los datos de un movimiento guardado.
    /// </summary>
    private void AddOrderRow(Order order)
    {
        // Si el cliente ya no existe, se muestra «—»; se añade a la lista del
        // desplegable para que la celda pueda pintarlo (mismo truco que
        // ClientForm con las provincias no oficiales).
        string client = _clientTextById.TryGetValue(order.ClientId, out string? text)
            ? text
            : "—";
        if (client == "—" && !clientColumn.Items.Contains("—"))
        {
            clientColumn.Items.Add("—");
        }

        int index = ordersDataGridView.Rows.Add(
            KindText(order.Kind),
            order.OrderDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            SpanishPrice.Format(order.Amount),
            order.Reference,
            order.Description,
            client);

        // El identificador del movimiento viaja en la fila sin mostrarse.
        ordersDataGridView.Rows[index].Tag = order.Id;
    }

    // Etiquetas en español del tipo de movimiento (constitución, punto 6);
    // coinciden con los elementos del desplegable del Designer.
    private static string KindText(OrderKind kind) => kind == OrderKind.CreditNote ? "Abono" : "Pedido";

    /// <summary>
    /// Rellena el desplegable de año con «Todos» y los años presentes en los
    /// movimientos visibles (specs/009 §4.2), conservando la selección si
    /// sigue existiendo.
    /// </summary>
    private void PopulateYearFilter(List<Order> orders)
    {
        string? selectedText = yearFilterComboBox.SelectedIndex >= 0
            ? yearFilterComboBox.SelectedItem?.ToString()
            : "Todos";

        yearFilterComboBox.Items.Clear();
        yearFilterComboBox.Items.Add("Todos");
        foreach (int year in orders.Select(order => order.OrderDate.Year).Distinct().OrderByDescending(year => year))
        {
            yearFilterComboBox.Items.Add(year.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        yearFilterComboBox.SelectedIndex = Math.Max(0,
            selectedText is null ? 0 : yearFilterComboBox.Items.IndexOf(selectedText));
    }

    /// <summary>
    /// Aplica los filtros del listado (specs/009 §§3-4.2): el texto busca en
    /// las columnas visibles (parcial, sin mayúsculas ni tildes) y el año
    /// deja ver solo el suyo; combinables con la vista filtrada por cliente.
    /// Las filas solo se ocultan: Guardar conserva también las ocultas, y si
    /// el movimiento marcado queda oculto, la marca se retira. Los totales
    /// se recalculan con lo visible.
    /// </summary>
    private void ApplyFilters()
    {
        if (_loading)
        {
            return;
        }

        string text = searchTextBox.Text;
        string yearText = yearFilterComboBox.SelectedItem?.ToString() ?? "Todos";
        bool allYears = yearText == "Todos";

        // Se recuerda el movimiento actual porque al soltar la celda o la
        // selección el evento SelectionChanged limpia la marca; se restaura
        // al final si su fila sigue visible.
        Guid? currentId = _currentOrderId;

        int visibleCount = 0;
        int totalCount = 0;
        var visibleOrders = new List<Order>();
        foreach (System.Windows.Forms.DataGridViewRow row in ordersDataGridView.Rows)
        {
            // Las filas vacías (solo posibles con edición abierta) se dejan ver.
            if (IsEmptyRow(row))
            {
                row.Visible = true;
                continue;
            }

            totalCount++;
            bool matches = (allYears || CellText(row, "orderDateColumn").EndsWith(
                $"/{yearText}", StringComparison.Ordinal))
                && OrderService.MatchesFilter(MapRowToOrder(row, out _), text, CellText(row, "clientColumn"));

            // La celda actual se suelta antes de ocultar su fila: el control
            // no permite ocultar la fila que la contiene.
            if (!matches && ordersDataGridView.CurrentCell?.OwningRow == row)
            {
                ordersDataGridView.CurrentCell = null;
                ordersDataGridView.ClearSelection();
            }

            row.Visible = matches;
            if (matches)
            {
                visibleCount++;
                visibleOrders.Add(MapRowToOrder(row, out _));
            }
        }

        counterLabel.Text = $"Mostrando {visibleCount} de {totalCount}";

        (int orderCount, decimal orderTotal, int creditNoteCount, decimal creditNoteTotal, decimal net) =
            OrderService.Totals(visibleOrders);
        totalsLabel.Text = allYears
            ? $"{orderCount} pedido(s) — {SpanishPrice.Format(orderTotal)} € · " +
                $"{creditNoteCount} abono(s) — −{SpanishPrice.Format(creditNoteTotal)} € · " +
                $"Neto: {SpanishPrice.Format(net)} €"
            : $"{yearText}: {orderCount} pedido(s) — {SpanishPrice.Format(orderTotal)} € · " +
                $"{creditNoteCount} abono(s) — −{SpanishPrice.Format(creditNoteTotal)} € · " +
                $"Neto: {SpanishPrice.Format(net)} €";

        // Si el movimiento actual quedó oculto por el filtro, la marca se
        // retira (volverá a marcarse con un clic cuando vuelva a verse).
        System.Windows.Forms.DataGridViewRow? currentRow = FindRowById(currentId);
        if (currentRow is null || !currentRow.Visible)
        {
            _currentOrderId = null;
            ordersDataGridView.ClearSelection();
        }
    }

    /// <summary>
    /// Vuelve a aplicar los filtros al cambiar el texto buscado (en vivo).
    /// </summary>
    private void SearchTextBox_TextChanged(object? sender, EventArgs e) => ApplyFilters();

    /// <summary>
    /// Vuelve a aplicar los filtros al cambiar el año del desplegable.
    /// </summary>
    private void YearFilterComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        ApplyFilters();
    }

    /// <summary>
    /// Actualiza los botones según el estado (normal o edición) y si se
    /// pueden validar y guardar datos (skill «modificar»): en edición, Nuevo,
    /// Modificar y Eliminar inactivos; Cancelar activo. El filtro de año
    /// también se bloquea durante la edición.
    /// </summary>
    private void UpdateButtonStates()
    {
        bool editing = _activeRow is not null;

        newButton.Enabled = _canSave && !editing;
        modifyButton.Enabled = _canSave && !editing;
        deleteButton.Enabled = _canSave && !editing;
        saveButton.Enabled = _canSave;
        cancelButton.Enabled = _canSave && editing;
        yearFilterComboBox.Enabled = _canSave && !editing;
        searchTextBox.Enabled = _canSave && !editing;
    }

    /// <summary>
    /// Inserta una fila vacía como primera fila con el valor por defecto
    /// (Tipo «Pedido»), la activa para edición y despliega el Tipo.
    /// </summary>
    private void NewButton_Click(object? sender, EventArgs e)
    {
        if (!_canSave || _activeRow is not null)
        {
            return;
        }

        ordersDataGridView.Rows.Insert(0, 1);
        System.Windows.Forms.DataGridViewRow row = ordersDataGridView.Rows[0];
        row.Cells["kindColumn"].Value = DefaultKindText;

        // Con el filtro activo, el movimiento nuevo nace vinculado al cliente
        // filtrado (todos los botones, pero solo para él, specs/009 §7).
        if (_filterClientId is not null)
        {
            row.Cells["clientColumn"].Value = _filterText;
        }

        ActivateRowEditing(row);

        // Selecciona la fila nueva y entra a editar la celda Tipo.
        ordersDataGridView.CurrentCell = row.Cells["kindColumn"];
        ordersDataGridView.BeginEdit(true);
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

        // Con el filtro activo, el cliente queda fijo en la fila editada.
        // Ojo con WinForms: poner una celda ReadOnly=false DESPUÉS de poner
        // su fila ReadOnly=true reactiva la fila, así que solo se toca aquí.
        row.Cells["clientColumn"].ReadOnly = _filterClientId is not null;

        // Durante la edición, el movimiento actual es el de esta fila (skill
        // «seleccionar-cliente», regla 6).
        if (row.Tag is Guid activeId)
        {
            _currentOrderId = activeId;
        }

        // Las demás filas quedan deshabilitadas: atenuadas en gris y sin
        // selección posible mientras dure la edición (skill «modificar»).
        foreach (System.Windows.Forms.DataGridViewRow other in ordersDataGridView.Rows)
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
        foreach (System.Windows.Forms.DataGridViewRow gridRow in ordersDataGridView.Rows)
        {
            gridRow.DefaultCellStyle.ForeColor = System.Drawing.SystemColors.ControlText;
        }
        SetColumnsSortable(true);
        UpdateButtonStates();
        ApplyFilters();
    }

    /// <summary>
    /// Activa o desactiva la ordenación por cabeceras (bloqueada durante la
    /// edición para no descuadrar la fila abierta). Las columnas de
    /// desplegable nunca ordenan.
    /// </summary>
    private void SetColumnsSortable(bool sortable)
    {
        foreach (System.Windows.Forms.DataGridViewColumn column in ordersDataGridView.Columns)
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
    /// Activa las celdas del movimiento marcado para reescribir las que se
    /// quieran (skill «modificar»).
    /// </summary>
    private void ModifyButton_Click(object? sender, EventArgs e)
    {
        if (!_canSave || _activeRow is not null)
        {
            return;
        }

        // Movimiento marcado con un clic en cualquier celda de su fila (skill
        // «seleccionar-cliente»): la fila se localiza por identificador.
        System.Windows.Forms.DataGridViewRow? row = FindRowById(_currentOrderId);
        if (row is null)
        {
            Prompt(
                "Selecciona un pedido o abono de la tabla.",
                "CRM1 — Modificar",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ActivateRowEditing(row);

        // Coloca el cursor en la primera celda de la fila.
        ordersDataGridView.CurrentCell = row.Cells["kindColumn"];
        ordersDataGridView.BeginEdit(true);
    }

    /// <summary>
    /// Elimina el movimiento marcado tras pedir confirmación identificado por
    /// su tipo, precio y cliente. La eliminación es en memoria: solo se hace
    /// permanente al pulsar Guardar (skill «borrar»; constitución, punto 5).
    /// </summary>
    private void DeleteButton_Click(object? sender, EventArgs e)
    {
        if (!_canSave || _activeRow is not null)
        {
            return;
        }

        // Movimiento marcado con un clic (skill «seleccionar-cliente»).
        System.Windows.Forms.DataGridViewRow? row = FindRowById(_currentOrderId);
        if (row is null)
        {
            Prompt(
                "Selecciona un pedido o abono de la tabla.",
                "CRM1 — Eliminar",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Confirmación antes de eliminar, identificando el movimiento por su
        // tipo, precio y cliente (specs/009 §4.1).
        DialogResult result = Prompt(
            $"¿Seguro que quieres eliminar el {RowDisplayName(row)}?\n\n" +
            "Se quitará de la tabla y se eliminará por completo de la base de datos al pulsar Guardar.",
            "CRM1 — Eliminar",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        // Eliminación en memoria: solo Guardar escribe en disco.
        ordersDataGridView.Rows.Remove(row);

        // Si la fila eliminada era la marcada, la marca se limpia (regla 5).
        EnsureCurrentOrderExists();

        // Queda un cambio sin guardar pendiente de Guardar.
        _dirty = true;

        // Los totales y el contador deben reflejar la fila retirada también
        // sin edición abierta.
        ApplyFilters();
    }

    /// <summary>
    /// Nombre visible de una fila de movimiento: «pedido de 1.200,00 € de
    /// (Cliente) Ana» o «abono de …», para identificarla sin ambigüedad.
    /// </summary>
    private static string RowDisplayName(System.Windows.Forms.DataGridViewRow row)
    {
        string kind = CellText(row, "kindColumn").ToLowerInvariant();
        string amount = CellText(row, "amountColumn");
        string client = CellText(row, "clientColumn");

        string prefix = kind == "abono" ? "abono" : "pedido";
        return $"{prefix} de {amount} €{(client.Length > 0 ? $" de {client}" : string.Empty)}";
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
        ordersDataGridView.EndEdit();

        System.Windows.Forms.DataGridViewRow row = _activeRow;

        if (row.Tag is not Guid)
        {
            // Fila creada con Nuevo y aún no guardada: se elimina.
            ordersDataGridView.Rows.Remove(row);
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
        EnsureCurrentOrderExists();

        EndEditingSession();
        ClearErrorMarks();

        // Recalcula si quedan otros cambios sin guardar (comparación con la
        // última copia guardada) para que el aviso al cerrar nunca mienta.
        _dirty = GridDiffersFromSaved();
        ApplyFilters();
    }

    /// <summary>
    /// Valida toda la tabla y, si todo es correcto, guarda los movimientos en
    /// la base de datos (specs/009).
    /// </summary>
    private void SaveButton_Click(object? sender, EventArgs e)
    {
        if (!_canSave)
        {
            return;
        }

        // Confirma la celda en edición para leer su último valor.
        ordersDataGridView.EndEdit();

        // Limpia las marcas rojas de una validación anterior.
        ClearErrorMarks();

        var errors = new List<(int Row, OrderValidationError Error)>();
        var orders = new List<Order>();
        var rowIndexes = new List<int>();
        var changedOrders = new List<Order>();
        var changedRowIndexes = new List<int>();

        // Copia guardada por Id: una fila idéntica a lo guardado es historial
        // que se reescribe tal cual — su fecha anterior a hoy sigue siendo
        // válida como historial (specs/009 §5); solo se validan los cambios.
        var savedById = _savedOrders.ToDictionary(saved => saved.Id);

        for (int i = 0; i < ordersDataGridView.Rows.Count; i++)
        {
            System.Windows.Forms.DataGridViewRow row = ordersDataGridView.Rows[i];

            // Las filas totalmente vacías se ignoran (p. ej. un Nuevo por
            // accidente); el Tipo «Pedido» por defecto no cuenta como dato.
            if (IsEmptyRow(row))
            {
                continue;
            }

            // Recorta los valores antes de validar y guardar.
            NormalizeRowCells(row);

            Order order = MapRowToOrder(row, out OrderValidationError? error);
            if (error is not null)
            {
                errors.Add((i, error));
                continue;
            }

            orders.Add(order);
            rowIndexes.Add(i);
            if (!(order.Id != Guid.Empty && savedById.TryGetValue(order.Id, out Order? saved) && OrdersEqual(order, saved)))
            {
                changedOrders.Add(order);
                changedRowIndexes.Add(i);
            }
        }

        // Reglas de negocio fuera del formulario (constitución, punto 3),
        // aplicadas a las filas nuevas o modificadas. La regla de «no anterior
        // a hoy» solo aplica al ALTA: una fila guardada que se modifica nunca
        // se bloquea por su fecha histórica (specs/009 §5).
        for (int i = 0; i < changedOrders.Count; i++)
        {
            bool isNew = !(changedOrders[i].Id != Guid.Empty && savedById.ContainsKey(changedOrders[i].Id));
            OrderValidationError? error = _orderService!.Validate(changedOrders[i], isNewCreation: isNew);
            if (error is not null)
            {
                errors.Add((changedRowIndexes[i], error));
            }
        }

        if (errors.Count > 0)
        {
            ShowValidationErrors(errors);
            return;
        }

        // Asigna identificadores a las filas que aún no tienen movimiento guardado.
        for (int i = 0; i < orders.Count; i++)
        {
            if (orders[i].Id == Guid.Empty)
            {
                orders[i].Id = Guid.NewGuid();
            }
            ordersDataGridView.Rows[rowIndexes[i]].Tag = orders[i].Id;
        }

        try
        {
            // Con el filtro activo, los movimientos ocultos se conservan:
            // guardar escribe los visibles más los de los demás clientes.
            List<Order> ordersToSave = _hiddenOrders.Concat(orders).ToList();
            _dataStore!.SaveOrders(ordersToSave);
        }
        catch (Exception ex)
        {
            // Un fallo de escritura nunca se presenta como éxito (constitución, punto 5).
            Prompt(
                "No se han podido guardar los pedidos y abonos.\n\n" + ex.Message,
                "CRM1 — Error al guardar",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _savedOrders = orders;
        _dirty = false;
        _lastDatabaseStamp = GetDatabaseStamp();

        // El movimiento actual pasa a ser el de la fila editada, ya con su
        // identificador asignado (skill «seleccionar-cliente», regla 6).
        if (_activeRow is not null && _activeRow.Tag is Guid savedId)
        {
            _currentOrderId = savedId;
        }

        // Todo vuelve a solo lectura y las filas vacías se retiran de la vista.
        RemoveEmptyRows();
        EndEditingSession();
        EnsureCurrentOrderExists();

        // La vista queda ordenada por fecha descendente (specs/009 §3).
        SortByDateDescending();

        ApplyFilters();

        Prompt(
            $"Se han guardado {orders.Count} pedido(s)/abono(s) correctamente.",
            "CRM1 — Guardar",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>
    /// Reordena la tabla por fecha descendente manteniendo la marca del
    /// movimiento actual sobre su fila (la vista queda siempre cronológica
    /// inversa, specs/009 §3).
    /// </summary>
    private void SortByDateDescending()
    {
        var byId = new Dictionary<Guid, System.Windows.Forms.DataGridViewRow>();
        foreach (System.Windows.Forms.DataGridViewRow row in ordersDataGridView.Rows)
        {
            if (row.Tag is Guid id)
            {
                byId[id] = row;
            }
        }

        Guid? currentId = _currentOrderId;

        // La ordenación se materializa ANTES de Clear: una fila separada del
        // control pierde la resolución de sus columnas por nombre.
        List<System.Windows.Forms.DataGridViewRow> sorted = byId.Values
            .OrderByDescending(row => ParseDateCell(row))
            .ToList();

        _loading = true;
        ordersDataGridView.Rows.Clear();
        foreach (System.Windows.Forms.DataGridViewRow row in sorted)
        {
            ordersDataGridView.Rows.Add(row);
        }
        _loading = false;

        // La marca sigue al mismo identificador donde haya quedado (regla 4 de
        // la skill «seleccionar-cliente»).
        _currentOrderId = currentId;
        EnsureCurrentOrderExists();
        System.Windows.Forms.DataGridViewRow? currentRow = FindRowById(currentId);
        if (currentRow is not null)
        {
            currentRow.Selected = true;
        }
    }

    // Fecha de una fila como DateTime (MinValue si no se puede leer).
    private static DateTime ParseDateCell(System.Windows.Forms.DataGridViewRow row)
    {
        string text = CellText(row, "orderDateColumn");
        return DateTime.TryParseExact(text, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date)
            ? date
            : DateTime.MinValue;
    }

    /// <summary>
    /// Retira de la vista las filas totalmente vacías (nunca se guardan).
    /// </summary>
    private void RemoveEmptyRows()
    {
        for (int i = ordersDataGridView.Rows.Count - 1; i >= 0; i--)
        {
            if (IsEmptyRow(ordersDataGridView.Rows[i]))
            {
                ordersDataGridView.Rows.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Indica si la tabla (filas no vacías) difiere de la última copia
    /// guardada, comparando por identificador para no depender del orden.
    /// </summary>
    private bool GridDiffersFromSaved()
    {
        var current = new List<Order>();
        foreach (System.Windows.Forms.DataGridViewRow row in ordersDataGridView.Rows)
        {
            if (IsEmptyRow(row))
            {
                continue;
            }
            current.Add(MapRowToOrder(row, out _));
        }

        if (current.Count != _savedOrders.Count)
        {
            return true;
        }

        var savedById = new Dictionary<Guid, Order>();
        foreach (Order order in _savedOrders)
        {
            savedById[order.Id] = order;
        }

        foreach (Order order in current)
        {
            if (!savedById.TryGetValue(order.Id, out Order? saved) || !OrdersEqual(order, saved))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Compara dos movimientos campo a campo.
    /// </summary>
    private static bool OrdersEqual(Order a, Order b)
    {
        return a.Kind == b.Kind
            && a.OrderDate == b.OrderDate
            && a.Amount == b.Amount
            && string.Equals(a.Reference, b.Reference, StringComparison.Ordinal)
            && string.Equals(a.Description, b.Description, StringComparison.Ordinal)
            && a.ClientId == b.ClientId;
    }

    /// <summary>
    /// Convierte una fila en un movimiento; los errores de forma (fecha,
    /// precio, tipo o cliente) se devuelven como error de validación.
    /// </summary>
    private Order MapRowToOrder(System.Windows.Forms.DataGridViewRow row, out OrderValidationError? error)
    {
        error = null;

        // Tipo: desplegable con dos valores.
        string kindText = CellText(row, "kindColumn");
        if (error is null && kindText is not ("Pedido" or "Abono"))
        {
            error = new OrderValidationError
            {
                Field = nameof(Order.Kind),
                Message = "El tipo no es válido."
            };
        }

        // Fecha en formato dd/MM/aaaa.
        DateTime orderDate = default;
        if (error is null)
        {
            string dateText = CellText(row, "orderDateColumn");
            if (dateText.Length == 0)
            {
                error = new OrderValidationError
                {
                    Field = nameof(Order.OrderDate),
                    Message = "La celda está vacía."
                };
            }
            else if (!DateTime.TryParseExact(dateText, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out orderDate))
            {
                error = new OrderValidationError
                {
                    Field = nameof(Order.OrderDate),
                    Message = "La fecha no es válida (dd/mm/aaaa)."
                };
            }
        }

        // Precio en formato español (specs/009 §5).
        decimal amount = 0m;
        if (error is null)
        {
            string amountText = CellText(row, "amountColumn");
            if (amountText.Length == 0)
            {
                error = new OrderValidationError
                {
                    Field = nameof(Order.Amount),
                    Message = "La celda está vacía."
                };
            }
            else if (!SpanishPrice.TryParse(amountText, out amount))
            {
                error = new OrderValidationError
                {
                    Field = nameof(Order.Amount),
                    Message = "El precio no es válido (usa el formato 1234,56)."
                };
            }
        }

        // Cliente: el texto del desplegable se traduce a Id; solo clientes.
        Guid clientId = Guid.Empty;
        string clientText = CellText(row, "clientColumn");
        if (error is null
            && (clientText.Length == 0 || clientText == "—" || !_clientByText.TryGetValue(clientText, out clientId)))
        {
            error = new OrderValidationError
            {
                Field = nameof(Order.ClientId),
                Message = "Debes elegir un cliente."
            };
        }

        return new Order
        {
            Id = row.Tag is Guid id ? id : Guid.Empty,
            Kind = kindText == "Abono" ? OrderKind.CreditNote : OrderKind.Order,
            OrderDate = orderDate,
            Amount = amount,
            Reference = CellText(row, "referenceColumn"),
            Description = CellText(row, "descriptionColumn"),
            ClientId = clientId
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
    /// Indica si todas las celdas de la fila están vacías. El Tipo «Pedido»
    /// por defecto de una fila nueva y el cliente pre-llenado por el filtro
    /// no cuentan como contenido, para poder descartar Nuevos por accidente.
    /// </summary>
    private bool IsEmptyRow(System.Windows.Forms.DataGridViewRow row)
    {
        foreach (System.Windows.Forms.DataGridViewCell cell in row.Cells)
        {
            string value = cell.Value?.ToString()?.Trim() ?? string.Empty;

            // El valor por defecto de Tipo se ignora.
            if (cell.OwningColumn.Name == "kindColumn" && value == DefaultKindText)
            {
                continue;
            }

            // El cliente pre-llenado por el filtro no cuenta como contenido.
            if (cell.OwningColumn.Name == "clientColumn" && value == _filterText)
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
    private void ShowValidationErrors(List<(int Row, OrderValidationError Error)> errors)
    {
        foreach (var (row, orderError) in errors)
        {
            string? columnName = ColumnNameFor(orderError.Field);
            if (columnName is not null)
            {
                _errorCells.Add(ordersDataGridView.Rows[row].Cells[columnName]);
            }
        }
        ordersDataGridView.Invalidate();

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
        ordersDataGridView.Invalidate();
    }

    /// <summary>
    /// Nombre de columna de la tabla para un campo del modelo.
    /// </summary>
    private static string? ColumnNameFor(string field) => field switch
    {
        nameof(Order.Kind) => "kindColumn",
        nameof(Order.OrderDate) => "orderDateColumn",
        nameof(Order.Amount) => "amountColumn",
        nameof(Order.Reference) => "referenceColumn",
        nameof(Order.Description) => "descriptionColumn",
        nameof(Order.ClientId) => "clientColumn",
        _ => null
    };

    /// <summary>
    /// Sello temporal de la base de datos: cambia cuando alguien más la escribe.
    /// </summary>
    private DateTime GetDatabaseStamp() =>
        File.Exists(_databaseFilePath) ? File.GetLastWriteTimeUtc(_databaseFilePath) : DateTime.MinValue;

    /// <summary>
    /// Al volver a mostrar el módulo, recarga los movimientos si la base de
    /// datos cambió en disco y aquí no hay nada pendiente (mismo criterio que
    /// ClientForm).
    /// </summary>
    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);

        if (Visible && _canSave && !_dirty && _activeRow is null
            && GetDatabaseStamp() != _lastDatabaseStamp)
        {
            LoadOrders();
        }
    }

    /// <summary>
    /// Muestra el error de datos y desactiva la gestión (constitución, punto 5).
    /// </summary>
    private void ReportDataError(Exception ex)
    {
        Prompt(
            "No se pudo leer la base de datos de pedidos (Data/crm1.db).\n\n" +
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
    private void OrdersDataGridView_CellValueChanged(object? sender, System.Windows.Forms.DataGridViewCellEventArgs e)
    {
        if (!_loading && e.RowIndex >= 0)
        {
            _dirty = true;
        }

        // Al corregir una celda marcada, su borde rojo desaparece al momento.
        if (e.RowIndex >= 0 && e.ColumnIndex >= 0
            && _errorCells.Remove(ordersDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex]))
        {
            ordersDataGridView.Invalidate();
        }
    }

    /// <summary>
    /// Marca que hay cambios sin guardar cuando se añade una fila; toda fila
    /// nueva nace en solo lectura (la activa se desbloquea al activarse).
    /// </summary>
    private void OrdersDataGridView_RowsAdded(object? sender, System.Windows.Forms.DataGridViewRowsAddedEventArgs e)
    {
        if (!_loading)
        {
            _dirty = true;
        }

        if (e.RowIndex >= 0)
        {
            ordersDataGridView.Rows[e.RowIndex].ReadOnly = true;
        }
    }

    /// <summary>
    /// Dibuja el borde rojo alrededor de las celdas con errores de validación.
    /// Las celdas permanecen siempre blancas para escribir.
    /// </summary>
    private void OrdersDataGridView_CellPainting(object? sender, System.Windows.Forms.DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        System.Windows.Forms.DataGridViewCell cell = ordersDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex];
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
    private void OrdersDataGridView_DataError(object? sender, System.Windows.Forms.DataGridViewDataErrorEventArgs e)
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
    private void OrdersDataGridView_SelectionChanged(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        System.Windows.Forms.DataGridViewRow? selected = ordersDataGridView.SelectedRows.Count > 0
            ? ordersDataGridView.SelectedRows[0]
            : null;

        if (_activeRow is not null && _activeRow.Index >= 0
            && !_restoringSelection && selected != _activeRow)
        {
            _restoringSelection = true;
            try
            {
                ordersDataGridView.ClearSelection();

                // Devuelve también la celda actual a la fila en edición,
                // manteniendo la columna en la que estaba el usuario.
                int column = ordersDataGridView.CurrentCell?.ColumnIndex ?? 0;
                if (column >= 0 && column < _activeRow.Cells.Count)
                {
                    ordersDataGridView.CurrentCell = _activeRow.Cells[column];
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

        // La fila seleccionada marca el movimiento actual (clic o teclado);
        // sin selección no hay movimiento actual, nunca un valor inventado.
        _currentOrderId = selected?.Tag is Guid orderId ? orderId : null;
    }

    /// <summary>
    /// Dibuja el borde grueso alrededor de la fila seleccionada, igual que en
    /// los demás módulos. Las celdas siguen siempre blancas.
    /// </summary>
    private void OrdersDataGridView_RowPostPaint(object? sender, System.Windows.Forms.DataGridViewRowPostPaintEventArgs e)
    {
        if (e.RowIndex < 0 || !ordersDataGridView.Rows[e.RowIndex].Selected)
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
    /// Busca la fila del movimiento con el identificador indicado (rastreo por
    /// Id, nunca por índice: admite ordenar o recargar la tabla).
    /// </summary>
    private System.Windows.Forms.DataGridViewRow? FindRowById(Guid? orderId)
    {
        if (orderId is null)
        {
            return null;
        }

        foreach (System.Windows.Forms.DataGridViewRow row in ordersDataGridView.Rows)
        {
            if (row.Tag is Guid id && id == orderId)
            {
                return row;
            }
        }

        return null;
    }

    /// <summary>
    /// Limpia la marca de movimiento actual si su fila ya no existe (regla 5
    /// de la skill «seleccionar-cliente»: sin identificadores huérfanos).
    /// </summary>
    private void EnsureCurrentOrderExists()
    {
        if (FindRowById(_currentOrderId) is null)
        {
            _currentOrderId = null;
            ordersDataGridView.ClearSelection();
        }
    }
}
