using System.Globalization;
using Crm1.WindowsForms.Models;
using Crm1.WindowsForms.Services;

namespace Crm1.WindowsForms.Forms;

/// <summary>
/// Formulario del módulo de presupuestos (specs/009): registro de que un
/// cliente o futuro cliente pidió un presupuesto —el documento vive en el
/// programa de administración— con su aceptación: al aceptar, el presupuesto
/// queda en el historial y el pedido se crea aparte heredando fecha, precio,
/// referencia y descripción. La lógica de negocio vive en QuoteService
/// (constitución, punto 3).
/// </summary>
public partial class QuoteForm : System.Windows.Forms.Form
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
    private const string DefaultStatusText = "Pendiente";

    // Almacén SQL con presupuestos y pedidos (specs/009 y specs/006).
    private readonly SqlDataStore? _dataStore;

    // Catálogo de códigos postales para la conversión en línea de un futuro
    // cliente al aceptar su presupuesto (specs/009 §4.3); null si falló.
    private readonly PostalCodeCatalog? _catalog;

    // Reglas de negocio de los presupuestos; null si la base no se pudo leer.
    private QuoteService? _quoteService;

    // Falso si la base de datos no se pudo abrir o leer: todos los botones
    // de gestión quedan inactivos (constitución, punto 5).
    private bool _canSave;

    // Fila activa para edición (rastreada por referencia); null si no hay sesión.
    private System.Windows.Forms.DataGridViewRow? _activeRow;

    // Instantánea de los valores de la fila activa al empezar la edición,
    // para restaurarlos si se pulsa Cancelar.
    private List<object?> _activeRowSnapshot = new();

    // Última copia guardada de los presupuestos visibles, para recalcular cambios.
    private List<Quote> _savedQuotes = new();

    // Celdas con errores de validación: reciben un borde rojo alrededor.
    private readonly HashSet<System.Windows.Forms.DataGridViewCell> _errorCells = new();

    // Verdadero mientras la tabla se rellena desde disco (no son cambios del usuario).
    private bool _loading;

    // Guarda anti-reentrada al devolver la selección a la fila en edición.
    private bool _restoringSelection;

    // Verdadero cuando hay cambios sin guardar.
    private bool _dirty;

    // Ruta y sello temporal de la base de datos: detecta presupuestos escritos
    // desde fuera para recargar al volver a mostrarse (mismo criterio que
    // ClientForm).
    private readonly string _databaseFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "crm1.db");
    private DateTime _lastDatabaseStamp;

    // Mapas del desplegable «Cliente/Futuro cliente»: texto visible ↔ registro.
    private readonly Dictionary<string, (Guid Id, TaskRelatedType Type)> _relatedByText = new();
    private readonly Dictionary<Guid, string> _relatedTextById = new();

    // Presupuesto actual marcado por el usuario (skill «seleccionar-cliente»):
    // rastreo por identificador, nunca por índice.
    private Guid? _currentQuoteId;

    // Filtro activo por registro vinculado (botón Presupuestos de ClientForm o
    // ContactForm, specs/009 §7); null = sin filtro.
    private TaskRelatedType? _filterType;
    private Guid? _filterId;
    private string _filterText = string.Empty;

    // Presupuestos ocultos por el filtro: se conservan en memoria y se guardan
    // junto con los visibles, así el filtro nunca hace perder datos.
    private List<Quote> _hiddenQuotes = new();

    // Pedido generado por cada presupuesto aceptado (OrderId de specs/009);
    // null = aún sin pedido. Se aplica al guardar.
    private readonly Dictionary<Guid, Guid?> _quoteOrderIds = new();

    // Pedidos nacidos de aceptaciones, pendientes de Guardar.
    private readonly List<Order> _pendingOrders = new();

    // Conversión en línea de un futuro cliente al aceptar su presupuesto
    // (specs/009 §4.3): se materializa al Guardar; null = sin conversión.
    private Contact? _conversionContact;
    private Client? _conversionClient;

    // Aceptaciones de esta sesión, con lo necesario para deshacerlas con
    // Cancelar (tercera capa de seguridad, como las conversiones de ContactForm).
    private sealed class AcceptUndo
    {
        public Guid QuoteId { get; init; }
        public string PreviousStatusText { get; init; } = string.Empty;
        public Guid? PreviousOrderId { get; init; }
        public string PreviousRelatedText { get; init; } = string.Empty;
    }

    private readonly List<AcceptUndo> _sessionAccepts = new();

    // Futuros a los que ya se ofreció la conversión en esta sesión (botón
    // Aceptar u oferta al Guardar): no se les vuelve a molestar.
    private readonly HashSet<Guid> _conversionOfferedProspects = new();

    /// <summary>
    /// Identificador del presupuesto actual (fila marcada con un clic), o null
    /// si no hay selección. Skill «seleccionar-cliente».
    /// </summary>
    public Guid? CurrentQuoteId => _currentQuoteId;

    /// <summary>
    /// Presupuesto actual reconstruido de la fila marcada, o null si no hay
    /// marca.
    /// </summary>
    public Quote? CurrentQuote => FindRowById(_currentQuoteId) is { } row
        ? MapRowToQuote(row, out _)
        : null;

    /// <summary>
    /// Verdadero cuando hay cambios sin guardar (aceptaciones incluidas). Lo
    /// consulta MainForm antes de cerrar la aplicación (specs/009 §7).
    /// </summary>
    public bool HasUnsavedChanges => _dirty;

    /// <summary>
    /// Verdadero mientras hay una fila en edición (Nuevo o Modificar). Lo usa
    /// la búsqueda global antes de navegar hasta un presupuesto (specs/004).
    /// </summary>
    public bool IsEditing => _activeRow is not null;

    /// <summary>
    /// Marca como presupuesto actual el del identificador indicado y desplaza
    /// la tabla hasta su fila (búsqueda global, specs/004). Devuelve false si
    /// hay una edición en curso o el presupuesto no está en la vista actual.
    /// </summary>
    public bool SelectQuoteById(Guid quoteId)
    {
        if (_activeRow is not null)
        {
            return false;
        }

        System.Windows.Forms.DataGridViewRow? row = FindRowById(quoteId);
        if (row is null)
        {
            return false;
        }

        quotesDataGridView.CurrentCell = row.Cells["quoteDateColumn"];
        quotesDataGridView.FirstDisplayedScrollingRowIndex = row.Index;
        return true;
    }

    /// <summary>
    /// Muestra solo los presupuestos del cliente o futuro cliente indicado,
    /// con el aviso «Presupuestos de: …» y el botón «Ver todas» (botón
    /// Presupuestos de ClientForm/ContactForm, specs/009 §7). Con edición o
    /// cambios pendientes no se cambia de vista (constitución, punto 5).
    /// Guardar conserva los presupuestos ocultos por el filtro.
    /// </summary>
    public void ShowQuotesOf(TaskRelatedType relatedType, Guid relatedId, string displayName)
    {
        if (_activeRow is not null || _dirty)
        {
            return;
        }

        _filterType = relatedType;
        _filterId = relatedId;
        _filterText = $"{(relatedType == TaskRelatedType.Client ? "(Cliente)" : "(Futuro cliente)")} {displayName}";
        filterLabel.Text = $"Presupuestos de: {_filterText}";
        filterLabel.Visible = true;
        showAllButton.Visible = true;
        LoadQuotes();
    }

    /// <summary>
    /// Quita el filtro y vuelve a mostrar todos los presupuestos.
    /// </summary>
    public void ShowAllQuotes()
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
        LoadQuotes();
    }

    /// <summary>
    /// Atiende el botón «Ver todas»: quita el filtro del listado.
    /// </summary>
    private void ShowAllButton_Click(object? sender, EventArgs e) => ShowAllQuotes();

    /// <summary>
    /// Diálogos del formulario (avisos, confirmaciones y errores): todo pasa
    /// por aquí. En la aplicación es un MessageBox con este formulario como
    /// dueño; las pruebas automatizadas de interfaz lo sustituyen por un
    /// delegado que responde sin bloquear.
    /// </summary>
    internal Func<string, string, MessageBoxButtons, MessageBoxIcon, DialogResult> Prompt { get; set; }

    /// <summary>
    /// Crea el formulario y carga los presupuestos guardados. Si la base de
    /// datos falla, la tabla queda vacía y los botones inactivos
    /// (constitución, punto 5).
    /// </summary>
    public QuoteForm() : this(null)
    {
    }

    /// <summary>
    /// Constructor con costura de pruebas: permite sustituir los diálogos
    /// antes de que la constructora cargue los datos, porque un error de base
    /// de datos durante la carga ya muestra un mensaje.
    /// </summary>
    internal QuoteForm(Func<string, string, MessageBoxButtons, MessageBoxIcon, DialogResult>? prompt)
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

        // Catálogo de códigos postales para la conversión en línea (§4.3); su
        // fallo no desactiva el módulo, solo la conversión al aceptar.
        try
        {
            _catalog = new PostalCodeCatalog(
                Path.Combine(AppContext.BaseDirectory, "Data", "postal-codes-es.json"));
        }
        catch
        {
            _catalog = null;
        }

        LoadQuotes();
    }

    /// <summary>
    /// Carga en la tabla los presupuestos guardados y prepara el desplegable
    /// de «Cliente/Futuro cliente». La vista queda ordenada por fecha
    /// descendente (lo más reciente arriba, specs/009 §3).
    /// </summary>
    private void LoadQuotes()
    {
        List<Quote> quotes;
        List<Client> clients;
        List<Contact> contacts;
        try
        {
            quotes = _dataStore!.LoadQuotes();
            clients = _dataStore.LoadClients();
            contacts = _dataStore.LoadContacts();
        }
        catch (Exception ex)
        {
            ReportDataError(ex);
            return;
        }

        _loading = true;
        quotesDataGridView.Rows.Clear();
        PopulateRelatedColumn(clients, contacts);

        // Con el filtro activo solo se muestran los presupuestos del registro
        // vinculado; los demás quedan ocultos y se conservan para Guardar.
        List<Quote> visible = quotes;
        if (_filterId is Guid filterId && _filterType is TaskRelatedType filterType)
        {
            visible = quotes.Where(quote => quote.RelatedId == filterId && quote.RelatedType == filterType).ToList();
            _hiddenQuotes = quotes.Where(quote => quote.RelatedId != filterId || quote.RelatedType != filterType).ToList();
        }
        else
        {
            _hiddenQuotes.Clear();
        }

        _quoteOrderIds.Clear();
        foreach (Quote quote in visible.OrderByDescending(quote => quote.QuoteDate))
        {
            AddQuoteRow(quote);
            _quoteOrderIds[quote.Id] = quote.OrderId;
        }

        _loading = false;
        _savedQuotes = visible;
        _dirty = false;
        _canSave = true;
        _pendingOrders.Clear();
        _conversionContact = null;
        _conversionClient = null;
        _sessionAccepts.Clear();
        _conversionOfferedProspects.Clear();
        _lastDatabaseStamp = GetDatabaseStamp();

        // Sin presupuesto actual hasta que el usuario marque una fila con un
        // clic (skill «seleccionar-cliente»).
        _currentQuoteId = null;
        quotesDataGridView.ClearSelection();
        PopulateYearFilter(visible);
        ApplyYearFilter();
        UpdateButtonStates();
    }

    /// <summary>
    /// Rellena el desplegable «Cliente/Futuro cliente» con la unión de
    /// clientes y futuros clientes, y construye los mapas texto ↔ registro
    /// que usa Guardar para reconstruir el vínculo. Si dos registros se
    /// llaman igual, el segundo añade « (2)» para que el texto sea único.
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
        _quoteService = new QuoteService(relatedRecords);
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

        // Las reglas deben conocer también los registros añadidos en marcha
        // (p. ej. el cliente que nace al aceptar el presupuesto de un futuro,
        // specs/009 §4.3): el servicio se reconstruye con el mapa completo.
        _quoteService = new QuoteService(_relatedByText.Values.ToDictionary(item => item.Id, item => item.Type));
    }

    // Nombre para mostrar: nombre y apellidos no vacíos (razón social en
    // empresas, apellidos ausentes en muchos futuros clientes).
    private static string DisplayName(string firstName, string lastName1, string lastName2) =>
        string.Join(" ", new[] { firstName, lastName1, lastName2 }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

    /// <summary>
    /// Añade a la tabla una fila con los datos de un presupuesto guardado.
    /// </summary>
    private void AddQuoteRow(Quote quote)
    {
        // Si el registro vinculado ya no existe, se muestra «—»; se añade a la
        // lista del desplegable para que la celda pueda pintarlo (mismo truco
        // que ClientForm con las provincias no oficiales).
        string related = _relatedTextById.TryGetValue(quote.RelatedId, out string? text)
            ? text
            : "—";
        if (related == "—" && !relatedColumn.Items.Contains("—"))
        {
            relatedColumn.Items.Add("—");
        }

        int index = quotesDataGridView.Rows.Add(
            quote.QuoteDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            SpanishPrice.Format(quote.Amount),
            quote.Reference,
            quote.Description,
            StatusText(quote.Status),
            related);

        // El identificador del presupuesto viaja en la fila sin mostrarse.
        quotesDataGridView.Rows[index].Tag = quote.Id;
    }

    // Etiquetas en español del estado (constitución, punto 6); coinciden con
    // los elementos del desplegable del Designer.
    private static string StatusText(QuoteStatus status) => status switch
    {
        QuoteStatus.Accepted => "Aceptado",
        QuoteStatus.Dismissed => "Descartado",
        _ => "Pendiente"
    };

    /// <summary>
    /// Rellena el desplegable de año con «Todos» y los años presentes en los
    /// presupuestos visibles (specs/009 §4.2), conservando la selección si
    /// sigue existiendo.
    /// </summary>
    private void PopulateYearFilter(List<Quote> quotes)
    {
        string? selectedText = quotesDataGridView is null || yearFilterComboBox.SelectedIndex < 0
            ? "Todos"
            : yearFilterComboBox.SelectedItem?.ToString();

        yearFilterComboBox.Items.Clear();
        yearFilterComboBox.Items.Add("Todos");
        foreach (int year in quotes.Select(quote => quote.QuoteDate.Year).Distinct().OrderByDescending(year => year))
        {
            yearFilterComboBox.Items.Add(year.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        yearFilterComboBox.SelectedIndex = Math.Max(0,
            selectedText is null ? 0 : yearFilterComboBox.Items.IndexOf(selectedText));
    }

    /// <summary>
    /// Aplica el filtro de año y refresca el contador y los totales con lo
    /// visible: «N presupuesto(s) — M aceptado(s)» según el año y el cliente
    /// filtrados (specs/009 §4.2).
    /// </summary>
    private void ApplyYearFilter()
    {
        string yearText = yearFilterComboBox.SelectedItem?.ToString() ?? "Todos";
        bool all = yearText == "Todos";

        int visibleCount = 0;
        int totalCount = 0;
        int acceptedCount = 0;
        foreach (System.Windows.Forms.DataGridViewRow row in quotesDataGridView.Rows)
        {
            if (IsEmptyRow(row))
            {
                row.Visible = true;
                continue;
            }

            totalCount++;
            bool matches = all || CellText(row, "quoteDateColumn").EndsWith(
                $"/{yearText}", StringComparison.Ordinal);
            row.Visible = matches;
            if (matches)
            {
                visibleCount++;
                if (CellText(row, "statusColumn") == "Aceptado")
                {
                    acceptedCount++;
                }
            }
        }

        counterLabel.Text = $"Mostrando {visibleCount} de {totalCount}";
        totalsLabel.Text = all
            ? $"{totalCount} presupuesto(s) — {acceptedCount} aceptado(s)"
            : $"{yearText}: {visibleCount} presupuesto(s) — {acceptedCount} aceptado(s)";
    }

    /// <summary>
    /// Vuelve a aplicar el filtro de año al cambiar el desplegable.
    /// </summary>
    private void YearFilterComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        ApplyYearFilter();
    }

    /// <summary>
    /// Actualiza los botones según el estado (normal o edición) y si se
    /// pueden validar y guardar datos (skill «modificar»). El filtro de año y
    /// Aceptar también se bloquean durante la edición.
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
        UpdateAcceptButton();
    }

    /// <summary>
    /// Ajusta el botón «Aceptar» según el presupuesto marcado (specs/009
    /// §4.3): «Aceptar» si es Pendiente, «Crear pedido» si es Aceptado sin
    /// pedido generado, inactivo en el resto de casos.
    /// </summary>
    private void UpdateAcceptButton()
    {
        if (!_canSave || _activeRow is not null)
        {
            acceptButton.Enabled = false;
            return;
        }

        System.Windows.Forms.DataGridViewRow? row = FindRowById(_currentQuoteId);
        if (row is null)
        {
            acceptButton.Enabled = false;
            return;
        }

        string status = CellText(row, "statusColumn");
        Guid quoteId = row.Tag is Guid id ? id : Guid.Empty;
        if (status == "Pendiente")
        {
            acceptButton.Text = "Aceptar";
            acceptButton.Enabled = true;
        }
        else if (status == "Aceptado"
            && (!_quoteOrderIds.TryGetValue(quoteId, out Guid? orderId) || orderId is null))
        {
            acceptButton.Text = "Crear pedido";
            acceptButton.Enabled = true;
        }
        else
        {
            acceptButton.Enabled = false;
        }
    }

    /// <summary>
    /// Inserta una fila vacía como primera fila con el valor por defecto
    /// (Estado «Pendiente»), la activa para edición y despliega la Fecha.
    /// </summary>
    private void NewButton_Click(object? sender, EventArgs e)
    {
        if (!_canSave || _activeRow is not null)
        {
            return;
        }

        quotesDataGridView.Rows.Insert(0, 1);
        System.Windows.Forms.DataGridViewRow row = quotesDataGridView.Rows[0];
        row.Cells["statusColumn"].Value = DefaultStatusText;

        // Con el filtro activo, el presupuesto nuevo nace vinculado al
        // registro filtrado (todos los botones, pero solo para él, §7).
        if (_filterId is not null)
        {
            row.Cells["relatedColumn"].Value = _filterText;
        }

        ActivateRowEditing(row);

        // Selecciona la fila nueva y entra a editar la celda Fecha.
        quotesDataGridView.CurrentCell = row.Cells["quoteDateColumn"];
        quotesDataGridView.BeginEdit(true);
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

        // Con el filtro activo, el vínculo queda fijo en la fila editada.
        // Ojo con WinForms: poner una celda ReadOnly=false DESPUÉS de poner
        // su fila ReadOnly=true reactiva la fila, así que solo se toca aquí.
        row.Cells["relatedColumn"].ReadOnly = _filterId is not null;

        // Durante la edición, el presupuesto actual es el de esta fila (skill
        // «seleccionar-cliente», regla 6).
        if (row.Tag is Guid activeId)
        {
            _currentQuoteId = activeId;
        }

        // Las demás filas quedan deshabilitadas: atenuadas en gris y sin
        // selección posible mientras dure la edición (skill «modificar»).
        foreach (System.Windows.Forms.DataGridViewRow other in quotesDataGridView.Rows)
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
        foreach (System.Windows.Forms.DataGridViewRow gridRow in quotesDataGridView.Rows)
        {
            gridRow.DefaultCellStyle.ForeColor = System.Drawing.SystemColors.ControlText;
        }
        SetColumnsSortable(true);
        UpdateButtonStates();
        ApplyYearFilter();
    }

    /// <summary>
    /// Activa o desactiva la ordenación por cabeceras (bloqueada durante la
    /// edición para no descuadrar la fila abierta). Las columnas de
    /// desplegable nunca ordenan.
    /// </summary>
    private void SetColumnsSortable(bool sortable)
    {
        foreach (System.Windows.Forms.DataGridViewColumn column in quotesDataGridView.Columns)
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
    /// Activa las celdas del presupuesto marcado para reescribir las que se
    /// quieran (skill «modificar»).
    /// </summary>
    private void ModifyButton_Click(object? sender, EventArgs e)
    {
        if (!_canSave || _activeRow is not null)
        {
            return;
        }

        // Presupuesto marcado con un clic (skill «seleccionar-cliente»).
        System.Windows.Forms.DataGridViewRow? row = FindRowById(_currentQuoteId);
        if (row is null)
        {
            Prompt(
                "Selecciona un presupuesto de la tabla.",
                "CRM1 — Modificar",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ActivateRowEditing(row);

        // Coloca el cursor en la primera celda de la fila.
        quotesDataGridView.CurrentCell = row.Cells["quoteDateColumn"];
        quotesDataGridView.BeginEdit(true);
    }

    /// <summary>
    /// Elimina el presupuesto marcado tras pedir confirmación identificado por
    /// su descripción o referencia. La eliminación es en memoria: solo se hace
    /// permanente al pulsar Guardar (skill «borrar»; constitución, punto 5).
    /// </summary>
    private void DeleteButton_Click(object? sender, EventArgs e)
    {
        if (!_canSave || _activeRow is not null)
        {
            return;
        }

        // Presupuesto marcado con un clic (skill «seleccionar-cliente»).
        System.Windows.Forms.DataGridViewRow? row = FindRowById(_currentQuoteId);
        if (row is null)
        {
            Prompt(
                "Selecciona un presupuesto de la tabla.",
                "CRM1 — Eliminar",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Confirmación antes de eliminar, identificando el presupuesto por su
        // descripción o referencia (specs/009 §4.1).
        DialogResult result = Prompt(
            $"¿Seguro que quieres eliminar el presupuesto {RowDisplayName(row)}?\n\n" +
            "Se quitará de la tabla y se eliminará por completo de la base de datos al pulsar Guardar.",
            "CRM1 — Eliminar",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        // Eliminación en memoria: solo Guardar escribe en disco.
        quotesDataGridView.Rows.Remove(row);

        // Si la fila eliminada era la marcada, la marca se limpia (regla 5).
        EnsureCurrentQuoteExists();

        // Queda un cambio sin guardar pendiente de Guardar.
        _dirty = true;

        // Totales y contador deben reflejar la fila retirada también sin
        // edición abierta.
        ApplyYearFilter();
        UpdateAcceptButton();
    }

    /// <summary>
    /// Nombre visible de una fila de presupuesto: su descripción o referencia
    /// (fecha y precio si no hay nada), para identificarla sin ambigüedad.
    /// </summary>
    private static string RowDisplayName(System.Windows.Forms.DataGridViewRow row)
    {
        string description = CellText(row, "descriptionColumn");
        if (description.Length > 0)
        {
            return $"\u0022{description}\u0022";
        }

        string reference = CellText(row, "referenceColumn");
        if (reference.Length > 0)
        {
            return $"con referencia {reference}";
        }

        return $"del {CellText(row, "quoteDateColumn")} por {CellText(row, "amountColumn")} €";
    }

    /// <summary>
    /// Acepta el presupuesto marcado (specs/009 §4.3): pasa a Aceptado y
    /// queda en el historial; el pedido se crea aparte heredando fecha,
    /// precio, referencia y descripción. Con «Crear pedido» (Aceptado sin
    /// pedido) repite solo la creación del pedido.
    /// </summary>
    private void AcceptButton_Click(object? sender, EventArgs e)
    {
        if (!_canSave || _activeRow is not null)
        {
            return;
        }

        System.Windows.Forms.DataGridViewRow? row = FindRowById(_currentQuoteId);
        if (row is null)
        {
            Prompt(
                "Selecciona un presupuesto de la tabla.",
                "CRM1 — Aceptar presupuesto",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Quote quote = MapRowToQuote(row, out QuoteValidationError? error);
        error ??= _quoteService!.Validate(quote);
        if (error is not null)
        {
            Prompt(
                "Corrige primero los datos del presupuesto:\n\n" + error.Message,
                "CRM1 — Aceptar presupuesto",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Identificador estable también para filas nuevas aún sin guardar.
        if (row.Tag is not Guid quoteId)
        {
            quoteId = Guid.NewGuid();
            row.Tag = quoteId;
        }
        quote.Id = quoteId;

        string relatedName = CellText(row, "relatedColumn");
        string dateText = quote.QuoteDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        string priceText = SpanishPrice.Format(quote.Amount);
        bool wasPending = CellText(row, "statusColumn") == "Pendiente";

        if (wasPending)
        {
            DialogResult confirm = Prompt(
                $"¿Aceptar el presupuesto de {relatedName} del {dateText} por {priceText} €?",
                "CRM1 — Aceptar presupuesto",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            _sessionAccepts.Add(new AcceptUndo
            {
                QuoteId = quoteId,
                PreviousStatusText = "Pendiente",
                PreviousOrderId = _quoteOrderIds.GetValueOrDefault(quoteId),
                PreviousRelatedText = relatedName
            });

            row.Cells["statusColumn"].Value = "Aceptado";
        }

        CreatePendingOrder(row, quote, relatedName);
    }

    /// <summary>
    /// Crea el pedido heredado del presupuesto aceptado: directamente si es de
    /// un cliente; si es de un futuro cliente, ofrece convertirlo en cliente
    /// en línea (specs/009 §4.3).
    /// </summary>
    private void CreatePendingOrder(System.Windows.Forms.DataGridViewRow row, Quote quote, string relatedName)
    {
        if (quote.RelatedType == TaskRelatedType.Client)
        {
            Order order = NewOrderFromQuote(quote, quote.RelatedId);
            _pendingOrders.Add(order);
            _quoteOrderIds[quote.Id] = order.Id;
            _dirty = true;

            ApplyYearFilter();
            UpdateAcceptButton();
            Prompt(
                "El presupuesto queda Aceptado y el pedido se creará al pulsar Guardar.",
                "CRM1 — Aceptar presupuesto",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Futuro cliente: sin datos de cliente no puede haber pedido; se
        // ofrece la conversión en línea con sus confirmaciones (§4.3).
        DialogResult convert = Prompt(
            $"Para crear el pedido, {relatedName} debe ser cliente con todos sus datos.\n\n¿Convertirlo ahora?",
            "CRM1 — Aceptar presupuesto",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        // Ya se le ha ofrecido en esta sesión: el Guardar no vuelve a molestar.
        _conversionOfferedProspects.Add(quote.RelatedId);

        if (convert != DialogResult.Yes)
        {
            UpdateAcceptButton();
            Prompt(
                "El presupuesto queda Aceptado sin pedido.\n\nCuando el futuro cliente sea cliente, " +
                "selecciónalo y pulsa «Crear pedido».",
                "CRM1 — Aceptar presupuesto",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ConvertProspectInline(row, quote, relatedName);
    }

    /// <summary>
    /// Conversión en línea del futuro cliente vinculado al presupuesto
    /// aceptado (specs/009 §4.3 y specs/003 §7): valida sus datos, pide las
    /// dos confirmaciones y deja la conversión en memoria hasta Guardar.
    /// </summary>
    private void ConvertProspectInline(System.Windows.Forms.DataGridViewRow row, Quote quote, string relatedName)
    {
        try
        {
            List<Contact> contacts = _dataStore!.LoadContacts();
            List<Client> clients = _dataStore.LoadClients();
            Contact? contact = contacts.FirstOrDefault(existing => existing.Id == quote.RelatedId);
            if (contact is null)
            {
                Prompt(
                    "No se encuentra el futuro cliente en la base de datos.\n\n" +
                    "El presupuesto queda Aceptado sin pedido.",
                    "CRM1 — Aceptar presupuesto",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_catalog is null)
            {
                Prompt(
                    "No se pudo cargar el catálogo de códigos postales; no se puede validar la conversión.\n\n" +
                    "El presupuesto queda Aceptado sin pedido.",
                    "CRM1 — Aceptar presupuesto",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var contactService = new ContactService(_catalog, clients.Select(client => client.TaxId));
            ConversionResult result = contactService.ConvertToClient(contact);
            if (!result.Success)
            {
                Prompt(
                    $"{relatedName} no puede convertirse en cliente todavía:\n\n" +
                    string.Join("\n", result.Errors) +
                    "\n\nEl presupuesto queda Aceptado sin pedido; completa sus datos y pulsa «Crear pedido».",
                    "CRM1 — Convertir en cliente",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Doble confirmación de la conversión (specs/003 §7).
            DialogResult first = Prompt(
                $"Vas a convertir a {relatedName} en cliente con estos datos:\n\n" +
                ClientSummary(result.Client!) + "\n\n¿Continuar?",
                "CRM1 — Convertir en cliente",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (first != DialogResult.Yes)
            {
                UpdateAcceptButton();
                return;
            }

            DialogResult second = Prompt(
                $"Al guardar, {relatedName} se dará de alta en Clientes, desaparecerá de Futuros clientes " +
                "y sus tareas, recordatorios y presupuestos pasarán a estar vinculados al cliente nuevo.\n\n" +
                "¿Confirmas la conversión?",
                "CRM1 — Confirmar conversión",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (second != DialogResult.Yes)
            {
                UpdateAcceptButton();
                return;
            }

            _conversionContact = contact;
            Client newClient = result.Client!;
            _conversionClient = newClient;

            // El presupuesto pasa a nombre del cliente nuevo también en la
            // vista; el registro se añade al desplegable si no estaba.
            string clientText = $"(Cliente) {DisplayName(contact.FirstName, contact.LastName1, contact.LastName2)}";
            if (!_relatedByText.ContainsKey(clientText))
            {
                AddRelatedItem(clientText, newClient.Id, TaskRelatedType.Client);
            }
            row.Cells["relatedColumn"].Value = _relatedTextById[newClient.Id];

            Order order = NewOrderFromQuote(quote, newClient.Id);
            _pendingOrders.Add(order);
            _quoteOrderIds[quote.Id] = order.Id;
            _dirty = true;

            ApplyYearFilter();
            UpdateAcceptButton();
            Prompt(
                "Al guardar se dará de alta al cliente, se reasignarán sus tareas, recordatorios y " +
                "presupuestos y se creará el pedido.",
                "CRM1 — Aceptar presupuesto",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            // Un fallo de lectura nunca se presenta como éxito (constitución, 5).
            Prompt(
                "No se pudo preparar la conversión del futuro cliente.\n\n" + ex.Message,
                "CRM1 — Error de datos",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // Pedido que nace de un presupuesto aceptado: hereda precio, referencia
    // (el número que los vincula) y descripción, y su fecha es el DÍA DE LA
    // ACEPTACIÓN — no la del presupuesto (decisión del usuario, 18/11,
    // specs/009 §4.3).
    private static Order NewOrderFromQuote(Quote quote, Guid clientId) => new()
    {
        Id = Guid.NewGuid(),
        Kind = OrderKind.Order,
        OrderDate = DateTime.Today,
        Amount = quote.Amount,
        Reference = quote.Reference,
        Description = quote.Description,
        ClientId = clientId
    };

    /// <summary>
    /// Al Guardar, detecta presupuestos que quedan Aceptados de un futuro
    /// cliente sin pedido —por cambio manual del Estado, fila nueva o un
    /// «No» anterior— y ofrece convertirlo en cliente en ese momento
    /// (specs/009 §4.3). A un futuro al que ya se ofreció en esta sesión no
    /// se le vuelve a molestar.
    /// </summary>
    private void OfferConversionForAcceptedProspectQuotes(List<Quote> quotes, List<int> rowIndexes)
    {
        var offeredHere = new HashSet<Guid>();
        for (int i = 0; i < quotes.Count; i++)
        {
            Quote quote = quotes[i];
            if (quote.Status != QuoteStatus.Accepted
                || quote.RelatedType != TaskRelatedType.Prospect
                || _quoteOrderIds.GetValueOrDefault(quote.Id) is not null
                || _conversionOfferedProspects.Contains(quote.RelatedId)
                || !offeredHere.Add(quote.RelatedId))
            {
                continue;
            }

            string relatedName = CellText(quotesDataGridView.Rows[rowIndexes[i]], "relatedColumn");
            string quoteName = quote.Description.Length > 0 ? quote.Description
                : quote.Reference.Length > 0 ? $"con referencia {quote.Reference}"
                : "sin descripción";

            DialogResult offer = Prompt(
                $"El presupuesto {quoteName} de {relatedName} está Aceptado.\n\n" +
                "Un futuro cliente no puede tener pedidos: para crear el suyo hay que convertirlo en cliente.\n\n" +
                "¿Convertirlo ahora?",
                "CRM1 — Convertir en cliente",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            _conversionOfferedProspects.Add(quote.RelatedId);
            if (offer != DialogResult.Yes)
            {
                continue;
            }

            ConvertAcceptedProspectQuotes(quotes, rowIndexes, quote.RelatedId, relatedName);
        }
    }

    /// <summary>
    /// Conversión en línea de un futuro cliente con presupuestos Aceptados
    /// sin pedido (oferta al Guardar): valida sus datos (carencias si
    /// faltan), pide la doble confirmación de specs/003 §7, revincula sus
    /// presupuestos al cliente nuevo y crea sus pedidos heredados, todo en
    /// memoria hasta el resto del Guardar en curso.
    /// </summary>
    private void ConvertAcceptedProspectQuotes(List<Quote> quotes, List<int> rowIndexes, Guid prospectId, string relatedName)
    {
        try
        {
            List<Contact> contacts = _dataStore!.LoadContacts();
            List<Client> clients = _dataStore.LoadClients();
            Contact? contact = contacts.FirstOrDefault(existing => existing.Id == prospectId);
            if (contact is null)
            {
                Prompt(
                    "No se encuentra el futuro cliente en la base de datos.\n\n" +
                    "El presupuesto quedará Aceptado sin pedido.",
                    "CRM1 — Convertir en cliente",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_catalog is null)
            {
                Prompt(
                    "No se pudo cargar el catálogo de códigos postales; no se puede validar la conversión.\n\n" +
                    "El presupuesto quedará Aceptado sin pedido.",
                    "CRM1 — Convertir en cliente",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var contactService = new ContactService(_catalog, clients.Select(client => client.TaxId));
            ConversionResult result = contactService.ConvertToClient(contact);
            if (!result.Success)
            {
                Prompt(
                    $"{relatedName} no puede convertirse en cliente todavía:\n\n" +
                    string.Join("\n", result.Errors) +
                    "\n\nEl presupuesto quedará Aceptado sin pedido; cuando completes sus datos, " +
                    "selecciónalo y pulsa «Crear pedido».",
                    "CRM1 — Convertir en cliente",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Doble confirmación de la conversión (specs/003 §7).
            DialogResult first = Prompt(
                $"Vas a convertir a {relatedName} en cliente con estos datos:\n\n" +
                ClientSummary(result.Client!) + "\n\n¿Continuar?",
                "CRM1 — Convertir en cliente",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (first != DialogResult.Yes)
            {
                return;
            }

            DialogResult second = Prompt(
                $"Al guardar, {relatedName} se dará de alta en Clientes, desaparecerá de Futuros clientes " +
                "y sus tareas, recordatorios y presupuestos pasarán a estar vinculados al cliente nuevo.\n\n" +
                "¿Confirmas la conversión?",
                "CRM1 — Confirmar conversión",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (second != DialogResult.Yes)
            {
                return;
            }

            _conversionContact = contact;
            Client newClient = result.Client!;
            _conversionClient = newClient;

            // El registro del cliente nuevo entra en el desplegable (esto
            // reconstruye QuoteService con él; ver specs/009 §4.3).
            string clientText = $"(Cliente) {DisplayName(contact.FirstName, contact.LastName1, contact.LastName2)}";
            if (!_relatedByText.ContainsKey(clientText))
            {
                AddRelatedItem(clientText, newClient.Id, TaskRelatedType.Client);
            }
            string newText = _relatedTextById[newClient.Id];

            // Todos los Aceptados sin pedido de ese futuro pasan al cliente
            // nuevo y crean su pedido heredado.
            for (int i = 0; i < quotes.Count; i++)
            {
                Quote candidate = quotes[i];
                if (candidate.RelatedId != prospectId
                    || candidate.Status != QuoteStatus.Accepted
                    || _quoteOrderIds.GetValueOrDefault(candidate.Id) is not null)
                {
                    continue;
                }

                quotesDataGridView.Rows[rowIndexes[i]].Cells["relatedColumn"].Value = newText;
                candidate.RelatedId = newClient.Id;
                candidate.RelatedType = TaskRelatedType.Client;

                Order order = NewOrderFromQuote(candidate, newClient.Id);
                _pendingOrders.Add(order);
                _quoteOrderIds[candidate.Id] = order.Id;
                candidate.OrderId = order.Id;
            }

            _dirty = true;
            Prompt(
                "Al guardar se dará de alta al cliente, se reasignarán sus tareas, recordatorios y " +
                "presupuestos y se crearán sus pedidos.",
                "CRM1 — Convertir en cliente",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            // Un fallo de lectura nunca se presenta como éxito (constitución, 5).
            Prompt(
                "No se pudo preparar la conversión del futuro cliente.\n\n" + ex.Message,
                "CRM1 — Error de datos",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // Resumen de los datos con los que se dará de alta un cliente convertido.
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
    /// Descarta la edición en curso: una fila nueva se elimina de la tabla y
    /// una modificada vuelve a los valores de la instantánea. Sin edición
    /// abierta, Cancelar deshace las aceptaciones pendientes (tercera capa de
    /// seguridad): pide confirmación y restaura los presupuestos.
    /// </summary>
    private void CancelButton_Click(object? sender, EventArgs e)
    {
        if (_activeRow is null)
        {
            if (_sessionAccepts.Count == 0 && _pendingOrders.Count == 0 && _conversionClient is null)
            {
                return;
            }

            DialogResult undo = Prompt(
                "Hay aceptaciones de presupuestos pendientes de guardar.\n\n" +
                "¿Deshacerlas y dejar los presupuestos como estaban?",
                "CRM1 — Cancelar aceptaciones",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (undo != DialogResult.Yes)
            {
                return;
            }

            foreach (AcceptUndo accept in _sessionAccepts)
            {
                System.Windows.Forms.DataGridViewRow? undoRow = FindRowById(accept.QuoteId);
                if (undoRow is null)
                {
                    continue;
                }

                undoRow.Cells["statusColumn"].Value = accept.PreviousStatusText;
                if (accept.PreviousRelatedText.Length > 0)
                {
                    undoRow.Cells["relatedColumn"].Value = accept.PreviousRelatedText;
                }
                _quoteOrderIds[accept.QuoteId] = accept.PreviousOrderId;
            }

            _sessionAccepts.Clear();
            _pendingOrders.Clear();
            _conversionContact = null;
            _conversionClient = null;
            _dirty = GridDiffersFromSaved();

            ApplyYearFilter();
            UpdateAcceptButton();
            return;
        }

        // Confirma la celda en edición antes de restaurar.
        quotesDataGridView.EndEdit();

        System.Windows.Forms.DataGridViewRow row = _activeRow;

        if (row.Tag is not Guid)
        {
            // Fila creada con Nuevo y aún no guardada: se elimina.
            quotesDataGridView.Rows.Remove(row);
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
        EnsureCurrentQuoteExists();

        EndEditingSession();
        ClearErrorMarks();

        // Recalcula si quedan otros cambios sin guardar (comparación con la
        // última copia guardada) para que el aviso al cerrar nunca mienta.
        _dirty = GridDiffersFromSaved() || _pendingOrders.Count > 0 || _conversionClient is not null;
        ApplyYearFilter();
    }

    /// <summary>
    /// Valida toda la tabla y, si todo es correcto, guarda los presupuestos
    /// —y los pedidos y la conversión pendientes— en la base de datos
    /// (specs/009 §4.3.7).
    /// </summary>
    private void SaveButton_Click(object? sender, EventArgs e)
    {
        if (!_canSave)
        {
            return;
        }

        // Confirma la celda en edición para leer su último valor.
        quotesDataGridView.EndEdit();

        // Limpia las marcas rojas de una validación anterior.
        ClearErrorMarks();

        var errors = new List<(int Row, QuoteValidationError Error)>();
        var quotes = new List<Quote>();
        var rowIndexes = new List<int>();
        var changedQuotes = new List<Quote>();
        var changedRowIndexes = new List<int>();

        // Copia guardada por Id: una fila idéntica a lo guardado es historial
        // que se reescribe tal cual — su fecha anterior a hoy sigue siendo
        // válida como historial (specs/009 §5); solo se validan los cambios.
        var savedById = _savedQuotes.ToDictionary(saved => saved.Id);

        for (int i = 0; i < quotesDataGridView.Rows.Count; i++)
        {
            System.Windows.Forms.DataGridViewRow row = quotesDataGridView.Rows[i];

            // Las filas totalmente vacías se ignoran (p. ej. un Nuevo por
            // accidente); el Estado «Pendiente» por defecto no cuenta como dato.
            if (IsEmptyRow(row))
            {
                continue;
            }

            // Recorta los valores antes de validar y guardar.
            NormalizeRowCells(row);

            Quote quote = MapRowToQuote(row, out QuoteValidationError? error);
            if (error is not null)
            {
                errors.Add((i, error));
                continue;
            }

            quotes.Add(quote);
            rowIndexes.Add(i);
            if (!(quote.Id != Guid.Empty && savedById.TryGetValue(quote.Id, out Quote? saved) && QuotesEqual(quote, saved)))
            {
                changedQuotes.Add(quote);
                changedRowIndexes.Add(i);
            }
        }

        // Reglas de negocio fuera del formulario (constitución, punto 3),
        // aplicadas a las filas nuevas o modificadas. La regla de «no anterior
        // a hoy» solo aplica al ALTA: una fila guardada que se modifica nunca
        // se bloquea por su fecha histórica (specs/009 §5).
        for (int i = 0; i < changedQuotes.Count; i++)
        {
            bool isNew = !(changedQuotes[i].Id != Guid.Empty && savedById.ContainsKey(changedQuotes[i].Id));
            QuoteValidationError? error = _quoteService!.Validate(changedQuotes[i], isNewCreation: isNew);
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

        // Asigna identificadores estables también a las filas nuevas, para
        // poder enlazar los pedidos que nazcan de la oferta de conversión.
        for (int i = 0; i < quotes.Count; i++)
        {
            if (quotes[i].Id == Guid.Empty)
            {
                quotes[i].Id = Guid.NewGuid();
            }
            quotesDataGridView.Rows[rowIndexes[i]].Tag = quotes[i].Id;
        }

        // Aceptados de futuro cliente sin pedido: ofrecer la conversión en
        // ese momento (cambio manual del Estado incluido, specs/009 §4.3).
        OfferConversionForAcceptedProspectQuotes(quotes, rowIndexes);

        // Aplica a cada presupuesto el pedido que tenga generado, incluidos
        // los creados por la oferta anterior.
        for (int i = 0; i < quotes.Count; i++)
        {
            quotes[i].OrderId = _quoteOrderIds.GetValueOrDefault(quotes[i].Id);
        }

        int createdOrders = _pendingOrders.Count;
        bool hadConversion = _conversionClient is not null;

        // Orden de escritura (specs/009 §4.3.7): primero la conversión (alta
        // del cliente, revinculación de tareas/recordatorios/presupuestos y
        // baja del futuro), luego los pedidos y por último los presupuestos;
        // cada fallo se muestra tal cual ocurrió y no se guarda nada más.
        if (_conversionClient is { } newClient && _conversionContact is { } convertedContact)
        {
            try
            {
                List<Client> clientsOnDisk = _dataStore!.LoadClients();
                clientsOnDisk.Add(newClient);
                _dataStore.SaveClients(clientsOnDisk);
            }
            catch (Exception ex)
            {
                Prompt(
                    "No se ha podido dar de alta el cliente convertido; no se ha guardado nada.\n\n" +
                    ex.Message,
                    "CRM1 — Error al guardar",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                _dataStore!.RelinkRelatedRecords(convertedContact.Id, newClient.Id);
            }
            catch (Exception ex)
            {
                Prompt(
                    "El cliente se ha dado de alta, pero sus tareas, recordatorios y presupuestos " +
                    "no se han podido reasignar; no se ha guardado nada más.\n\n" +
                    ex.Message,
                    "CRM1 — Error al guardar",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                List<Contact> contactsOnDisk = _dataStore!.LoadContacts();
                contactsOnDisk.RemoveAll(existing => existing.Id == convertedContact.Id);
                _dataStore.SaveContacts(contactsOnDisk);
            }
            catch (Exception ex)
            {
                Prompt(
                    "El cliente se ha dado de alta y sus registros reasignados, pero el futuro cliente " +
                    "sigue en Futuros clientes; no se ha guardado nada más.\n\n" +
                    ex.Message,
                    "CRM1 — Error al guardar",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        if (_pendingOrders.Count > 0)
        {
            try
            {
                List<Order> ordersOnDisk = _dataStore!.LoadOrders();
                ordersOnDisk.AddRange(_pendingOrders);
                _dataStore.SaveOrders(ordersOnDisk);
            }
            catch (Exception ex)
            {
                Prompt(
                    (hadConversion
                        ? "El cliente se ha dado de alta y reasignado, pero los pedidos no se han podido crear; "
                        : "No se han podido crear los pedidos de los presupuestos aceptados; ") +
                    "no se ha guardado nada más.\n\n" +
                    ex.Message,
                    "CRM1 — Error al guardar",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        try
        {
            // Con el filtro activo, los presupuestos ocultos se conservan.
            List<Quote> quotesToSave = _hiddenQuotes.Concat(quotes).ToList();
            _dataStore!.SaveQuotes(quotesToSave);
        }
        catch (Exception ex)
        {
            // Un fallo de escritura nunca se presenta como éxito (constitución, punto 5).
            Prompt(
                "No se han podido guardar los presupuestos.\n\n" + ex.Message,
                "CRM1 — Error al guardar",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _savedQuotes = quotes;
        _dirty = false;
        _lastDatabaseStamp = GetDatabaseStamp();
        _pendingOrders.Clear();
        _conversionContact = null;
        _conversionClient = null;
        _sessionAccepts.Clear();

        // Con conversión, la vista entera se recarga de la base: el futuro ya
        // no existe y sus presupuestos apuntan al cliente nuevo.
        if (hadConversion)
        {
            LoadQuotes();
        }
        else
        {
            // El presupuesto actual pasa a ser el de la fila editada, ya con
            // su identificador asignado (skill «seleccionar-cliente», regla 6).
            if (_activeRow is not null && _activeRow.Tag is Guid savedId)
            {
                _currentQuoteId = savedId;
            }

            // Todo vuelve a solo lectura y las filas vacías se retiran de la vista.
            RemoveEmptyRows();
            EndEditingSession();
            EnsureCurrentQuoteExists();

            // La vista queda ordenada por fecha descendente (specs/009 §3).
            SortByDateDescending();
            ApplyYearFilter();
        }

        Prompt(
            $"Se han guardado {quotes.Count} presupuesto(s) correctamente." +
            (createdOrders > 0 ? $"\nSe han creado {createdOrders} pedido(s)." : string.Empty) +
            (hadConversion ? "\nSe ha dado de alta al futuro cliente como cliente." : string.Empty),
            "CRM1 — Guardar",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>
    /// Reordena la tabla por fecha descendente manteniendo la marca del
    /// presupuesto actual sobre su fila (la vista queda siempre cronológica
    /// inversa, specs/009 §3).
    /// </summary>
    private void SortByDateDescending()
    {
        var byId = new Dictionary<Guid, System.Windows.Forms.DataGridViewRow>();
        foreach (System.Windows.Forms.DataGridViewRow row in quotesDataGridView.Rows)
        {
            if (row.Tag is Guid id)
            {
                byId[id] = row;
            }
        }

        Guid? currentId = _currentQuoteId;

        // La ordenación se materializa ANTES de Clear: una fila separada del
        // control pierde la resolución de sus columnas por nombre.
        List<System.Windows.Forms.DataGridViewRow> sorted = byId.Values
            .OrderByDescending(row => ParseDateCell(row))
            .ToList();

        _loading = true;
        quotesDataGridView.Rows.Clear();
        foreach (System.Windows.Forms.DataGridViewRow row in sorted)
        {
            quotesDataGridView.Rows.Add(row);
        }
        _loading = false;

        // La marca sigue al mismo identificador donde haya quedado (regla 4 de
        // la skill «seleccionar-cliente»).
        _currentQuoteId = currentId;
        EnsureCurrentQuoteExists();
        System.Windows.Forms.DataGridViewRow? currentRow = FindRowById(currentId);
        if (currentRow is not null)
        {
            currentRow.Selected = true;
        }
    }

    // Fecha de una fila como DateTime (MinValue si no se puede leer).
    private static DateTime ParseDateCell(System.Windows.Forms.DataGridViewRow row)
    {
        string text = CellText(row, "quoteDateColumn");
        return DateTime.TryParseExact(text, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date)
            ? date
            : DateTime.MinValue;
    }

    /// <summary>
    /// Retira de la vista las filas totalmente vacías (nunca se guardan).
    /// </summary>
    private void RemoveEmptyRows()
    {
        for (int i = quotesDataGridView.Rows.Count - 1; i >= 0; i--)
        {
            if (IsEmptyRow(quotesDataGridView.Rows[i]))
            {
                quotesDataGridView.Rows.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Indica si la tabla (filas no vacías) difiere de la última copia
    /// guardada, comparando por identificador para no depender del orden.
    /// </summary>
    private bool GridDiffersFromSaved()
    {
        var current = new List<Quote>();
        foreach (System.Windows.Forms.DataGridViewRow row in quotesDataGridView.Rows)
        {
            if (IsEmptyRow(row))
            {
                continue;
            }
            current.Add(MapRowToQuote(row, out _));
        }

        if (current.Count != _savedQuotes.Count)
        {
            return true;
        }

        var savedById = new Dictionary<Guid, Quote>();
        foreach (Quote quote in _savedQuotes)
        {
            savedById[quote.Id] = quote;
        }

        foreach (Quote quote in current)
        {
            if (!savedById.TryGetValue(quote.Id, out Quote? saved) || !QuotesEqual(quote, saved))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Compara dos presupuestos campo a campo.
    /// </summary>
    private static bool QuotesEqual(Quote a, Quote b)
    {
        return a.QuoteDate == b.QuoteDate
            && a.Amount == b.Amount
            && string.Equals(a.Reference, b.Reference, StringComparison.Ordinal)
            && string.Equals(a.Description, b.Description, StringComparison.Ordinal)
            && a.Status == b.Status
            && a.RelatedId == b.RelatedId
            && a.RelatedType == b.RelatedType
            && a.OrderId == b.OrderId;
    }

    /// <summary>
    /// Convierte una fila en un presupuesto; los errores de forma (fecha,
    /// precio, estado o vínculo) se devuelven como error de validación.
    /// </summary>
    private Quote MapRowToQuote(System.Windows.Forms.DataGridViewRow row, out QuoteValidationError? error)
    {
        error = null;

        // Fecha en formato dd/MM/aaaa.
        DateTime quoteDate = default;
        string dateText = CellText(row, "quoteDateColumn");
        if (dateText.Length == 0)
        {
            error = new QuoteValidationError
            {
                Field = nameof(Quote.QuoteDate),
                Message = "La celda está vacía."
            };
        }
        else if (!DateTime.TryParseExact(dateText, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out quoteDate))
        {
            error = new QuoteValidationError
            {
                Field = nameof(Quote.QuoteDate),
                Message = "La fecha no es válida (dd/mm/aaaa)."
            };
        }

        // Precio en formato español (specs/009 §5).
        decimal amount = 0m;
        if (error is null)
        {
            string amountText = CellText(row, "amountColumn");
            if (amountText.Length == 0)
            {
                error = new QuoteValidationError
                {
                    Field = nameof(Quote.Amount),
                    Message = "La celda está vacía."
                };
            }
            else if (!SpanishPrice.TryParse(amountText, out amount))
            {
                error = new QuoteValidationError
                {
                    Field = nameof(Quote.Amount),
                    Message = "El precio no es válido (usa el formato 1234,56)."
                };
            }
        }

        // Estado: desplegable con tres valores.
        string statusText = CellText(row, "statusColumn");
        QuoteStatus status = statusText switch
        {
            "Aceptado" => QuoteStatus.Accepted,
            "Descartado" => QuoteStatus.Dismissed,
            "Pendiente" => QuoteStatus.Pending,
            _ => QuoteStatus.Pending
        };
        if (error is null && statusText is not ("Pendiente" or "Aceptado" or "Descartado"))
        {
            error = new QuoteValidationError
            {
                Field = nameof(Quote.Status),
                Message = "El estado no es válido."
            };
        }

        // Vínculo: el texto del desplegable se traduce a (Id, tipo).
        Guid relatedId = Guid.Empty;
        TaskRelatedType relatedType = TaskRelatedType.Client;
        string relatedText = CellText(row, "relatedColumn");
        if (error is null)
        {
            if (relatedText.Length == 0 || relatedText == "—"
                || !_relatedByText.TryGetValue(relatedText, out (Guid Id, TaskRelatedType Type) related))
            {
                error = new QuoteValidationError
                {
                    Field = nameof(Quote.RelatedId),
                    Message = "Debes elegir un cliente o futuro cliente."
                };
            }
            else
            {
                relatedId = related.Id;
                relatedType = related.Type;
            }
        }

        return new Quote
        {
            Id = row.Tag is Guid id ? id : Guid.Empty,
            QuoteDate = quoteDate,
            Amount = amount,
            Reference = CellText(row, "referenceColumn"),
            Description = CellText(row, "descriptionColumn"),
            Status = status,
            RelatedId = relatedId,
            RelatedType = relatedType,
            OrderId = _quoteOrderIds.GetValueOrDefault(row.Tag is Guid quoteId ? quoteId : Guid.Empty)
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
    /// Indica si todas las celdas de la fila están vacías. El Estado
    /// «Pendiente» por defecto de una fila nueva y el vínculo pre-llenado por
    /// el filtro no cuentan como contenido, para poder descartar Nuevos por
    /// accidente.
    /// </summary>
    private bool IsEmptyRow(System.Windows.Forms.DataGridViewRow row)
    {
        foreach (System.Windows.Forms.DataGridViewCell cell in row.Cells)
        {
            string value = cell.Value?.ToString()?.Trim() ?? string.Empty;

            // El valor por defecto de Estado se ignora.
            if (cell.OwningColumn.Name == "statusColumn" && value == DefaultStatusText)
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
    private void ShowValidationErrors(List<(int Row, QuoteValidationError Error)> errors)
    {
        foreach (var (row, quoteError) in errors)
        {
            string? columnName = ColumnNameFor(quoteError.Field);
            if (columnName is not null)
            {
                _errorCells.Add(quotesDataGridView.Rows[row].Cells[columnName]);
            }
        }
        quotesDataGridView.Invalidate();

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
        quotesDataGridView.Invalidate();
    }

    /// <summary>
    /// Nombre de columna de la tabla para un campo del modelo.
    /// </summary>
    private static string? ColumnNameFor(string field) => field switch
    {
        nameof(Quote.QuoteDate) => "quoteDateColumn",
        nameof(Quote.Amount) => "amountColumn",
        nameof(Quote.Reference) => "referenceColumn",
        nameof(Quote.Description) => "descriptionColumn",
        nameof(Quote.Status) => "statusColumn",
        nameof(Quote.RelatedId) => "relatedColumn",
        _ => null
    };

    /// <summary>
    /// Sello temporal de la base de datos: cambia cuando alguien más la escribe.
    /// </summary>
    private DateTime GetDatabaseStamp() =>
        File.Exists(_databaseFilePath) ? File.GetLastWriteTimeUtc(_databaseFilePath) : DateTime.MinValue;

    /// <summary>
    /// Al volver a mostrar el módulo, recarga los presupuestos si la base de
    /// datos cambió en disco y aquí no hay nada pendiente (mismo criterio que
    /// ClientForm).
    /// </summary>
    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);

        if (Visible && _canSave && !_dirty && _activeRow is null
            && GetDatabaseStamp() != _lastDatabaseStamp)
        {
            LoadQuotes();
        }
    }

    /// <summary>
    /// Muestra el error de datos y desactiva la gestión (constitución, punto 5).
    /// </summary>
    private void ReportDataError(Exception ex)
    {
        Prompt(
            "No se pudo leer la base de datos de presupuestos (Data/crm1.db).\n\n" +
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
    private void QuotesDataGridView_CellValueChanged(object? sender, System.Windows.Forms.DataGridViewCellEventArgs e)
    {
        if (!_loading && e.RowIndex >= 0)
        {
            _dirty = true;
        }

        // Al corregir una celda marcada, su borde rojo desaparece al momento.
        if (e.RowIndex >= 0 && e.ColumnIndex >= 0
            && _errorCells.Remove(quotesDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex]))
        {
            quotesDataGridView.Invalidate();
        }
    }

    /// <summary>
    /// Marca que hay cambios sin guardar cuando se añade una fila; toda fila
    /// nueva nace en solo lectura (la activa se desbloquea al activarse).
    /// </summary>
    private void QuotesDataGridView_RowsAdded(object? sender, System.Windows.Forms.DataGridViewRowsAddedEventArgs e)
    {
        if (!_loading)
        {
            _dirty = true;
        }

        if (e.RowIndex >= 0)
        {
            quotesDataGridView.Rows[e.RowIndex].ReadOnly = true;
        }
    }

    /// <summary>
    /// Dibuja el borde rojo alrededor de las celdas con errores de validación.
    /// Las celdas permanecen siempre blancas para escribir.
    /// </summary>
    private void QuotesDataGridView_CellPainting(object? sender, System.Windows.Forms.DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        System.Windows.Forms.DataGridViewCell cell = quotesDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex];
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
    private void QuotesDataGridView_DataError(object? sender, System.Windows.Forms.DataGridViewDataErrorEventArgs e)
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
    private void QuotesDataGridView_SelectionChanged(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        System.Windows.Forms.DataGridViewRow? selected = quotesDataGridView.SelectedRows.Count > 0
            ? quotesDataGridView.SelectedRows[0]
            : null;

        if (_activeRow is not null && _activeRow.Index >= 0
            && !_restoringSelection && selected != _activeRow)
        {
            _restoringSelection = true;
            try
            {
                quotesDataGridView.ClearSelection();

                // Devuelve también la celda actual a la fila en edición,
                // manteniendo la columna en la que estaba el usuario.
                int column = quotesDataGridView.CurrentCell?.ColumnIndex ?? 0;
                if (column >= 0 && column < _activeRow.Cells.Count)
                {
                    quotesDataGridView.CurrentCell = _activeRow.Cells[column];
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

        // La fila seleccionada marca el presupuesto actual (clic o teclado);
        // sin selección no hay presupuesto actual, nunca un valor inventado.
        _currentQuoteId = selected?.Tag is Guid quoteId ? quoteId : null;
        UpdateAcceptButton();
    }

    /// <summary>
    /// Dibuja el borde grueso alrededor de la fila seleccionada, igual que en
    /// los demás módulos. Las celdas siguen siempre blancas.
    /// </summary>
    private void QuotesDataGridView_RowPostPaint(object? sender, System.Windows.Forms.DataGridViewRowPostPaintEventArgs e)
    {
        if (e.RowIndex < 0 || !quotesDataGridView.Rows[e.RowIndex].Selected)
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
    /// Busca la fila del presupuesto con el identificador indicado (rastreo
    /// por Id, nunca por índice: admite ordenar o recargar la tabla).
    /// </summary>
    private System.Windows.Forms.DataGridViewRow? FindRowById(Guid? quoteId)
    {
        if (quoteId is null)
        {
            return null;
        }

        foreach (System.Windows.Forms.DataGridViewRow row in quotesDataGridView.Rows)
        {
            if (row.Tag is Guid id && id == quoteId)
            {
                return row;
            }
        }

        return null;
    }

    /// <summary>
    /// Limpia la marca de presupuesto actual si su fila ya no existe (regla 5
    /// de la skill «seleccionar-cliente»: sin identificadores huérfanos).
    /// </summary>
    private void EnsureCurrentQuoteExists()
    {
        if (FindRowById(_currentQuoteId) is null)
        {
            _currentQuoteId = null;
            quotesDataGridView.ClearSelection();
        }
    }
}
