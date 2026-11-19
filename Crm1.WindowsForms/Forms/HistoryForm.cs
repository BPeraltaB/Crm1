using System.Globalization;
using Crm1.WindowsForms.Models;
using Crm1.WindowsForms.Services;

namespace Crm1.WindowsForms.Forms;

/// <summary>
/// Formulario del módulo de historial (specs/010-historial.md): una sola
/// vista cronológica por registro con sus tareas, recordatorios,
/// presupuestos y pedidos/abonos, lo más reciente arriba. Es de **solo
/// lectura**: no hay edición ni guardado, y la agregación vive en
/// HistoryService (constitución, punto 3).
/// </summary>
public partial class HistoryForm : System.Windows.Forms.Form
{
    // Color y grosor del borde que marca la fila seleccionada, igual que en
    // los demás módulos (skill «seleccionar-cliente»).
    private static readonly System.Drawing.Color SelectionBorderColor = System.Drawing.Color.FromArgb(100, 116, 139);
    private const float SelectionBorderWidth = 3F;

    // Almacén SQL con todos los datos agregados; null si la base no se pudo
    // abrir (la tabla queda vacía, constitución, punto 5).
    private readonly SqlDataStore? _dataStore;

    // Registro objetivo de la vista (specs/010 §3): el historial siempre
    // está filtrado; sin registro no hay nada que mostrar.
    private TaskRelatedType _filterType;
    private Guid? _filterId;
    private string _filterText = string.Empty;

    // Ruta y sello temporal de la base de datos: detecta escrituras de otros
    // módulos para recargar al volver a mostrarse (mismo criterio que TaskForm).
    private readonly string _databaseFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "crm1.db");
    private DateTime _lastDatabaseStamp;

    /// <summary>
    /// Diálogos del formulario (avisos y errores): todo pasa por aquí. En la
    /// aplicación es un MessageBox con este formulario como dueño; las
    /// pruebas automatizadas de interfaz lo sustituyen por un delegado que
    /// responde sin bloquear.
    /// </summary>
    internal Func<string, string, MessageBoxButtons, MessageBoxIcon, DialogResult> Prompt { get; set; }

    /// <summary>
    /// Crea el formulario vacío: la vista se fija con ShowHistoryOf al abrirlo
    /// desde el botón «Historial» de ClientForm o ContactForm (specs/010 §4).
    /// </summary>
    public HistoryForm() : this(null)
    {
    }

    /// <summary>
    /// Constructor con costura de pruebas: permite sustituir los diálogos
    /// antes de que la constructora toque la base de datos, porque un error
    /// de apertura ya muestra un mensaje.
    /// </summary>
    internal HistoryForm(Func<string, string, MessageBoxButtons, MessageBoxIcon, DialogResult>? prompt)
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
        }
    }

    /// <summary>
    /// Muestra el historial del cliente o futuro cliente indicado, con el
    /// aviso «Historial de: …» (specs/010 §3). Siempre filtrado: no hay
    /// botón «Ver todas/os» ni vista general.
    /// </summary>
    public void ShowHistoryOf(TaskRelatedType relatedType, Guid relatedId, string displayName)
    {
        _filterType = relatedType;
        _filterId = relatedId;
        _filterText = $"{(relatedType == TaskRelatedType.Client ? "(Cliente)" : "(Futuro cliente)")} {displayName}";
        filterLabel.Text = $"Historial de: {_filterText}";
        filterLabel.Visible = true;
        LoadHistory();
    }

    /// <summary>
    /// Recarga de la base las listas de todos los módulos, agrega el
    /// historial del registro objetivo con HistoryService y rellena la tabla,
    /// el contador y la barra de totales (specs/010 §2 y §3).
    /// </summary>
    private void LoadHistory()
    {
        if (_dataStore is null || _filterId is null)
        {
            return;
        }

        List<Models.Task> tasks;
        List<Reminder> reminders;
        List<Quote> quotes;
        List<Order> orders;
        List<Client> clients;
        List<Contact> contacts;
        try
        {
            tasks = _dataStore.LoadTasks();
            reminders = _dataStore.LoadReminders();
            quotes = _dataStore.LoadQuotes();
            orders = _dataStore.LoadOrders();
            clients = _dataStore.LoadClients();
            contacts = _dataStore.LoadContacts();
        }
        catch (Exception ex)
        {
            ReportDataError(ex);
            return;
        }

        // Registro objetivo: exactamente uno de los dos según el filtro.
        Client? client = _filterType == TaskRelatedType.Client
            ? clients.FirstOrDefault(item => item.Id == _filterId)
            : null;
        Contact? contact = _filterType == TaskRelatedType.Prospect
            ? contacts.FirstOrDefault(item => item.Id == _filterId)
            : null;

        HistoryResult history = HistoryService.Build(
            client, contact, tasks, reminders, quotes, orders);

        historyDataGridView.Rows.Clear();
        foreach (HistoryEntry entry in history.Entries)
        {
            historyDataGridView.Rows.Add(
                FormatDate(entry),
                entry.TypeText,
                entry.Detail,
                entry.StatusText);
        }

        // Sin fila marcada: aquí no hay «registro actual», solo lectura.
        historyDataGridView.ClearSelection();
        counterLabel.Text = $"Mostrando {history.Entries.Count} de {history.Entries.Count}";
        totalsLabel.Text = TotalsText(history.Totals);
        _lastDatabaseStamp = GetDatabaseStamp();
    }

    // Fecha de una línea: dd/MM/aaaa, con hora en los recordatorios
    // (specs/010 §3), cuya hora importa; las de resumen anual se muestran
    // solo con el año («2026») porque encabezan su bloque.
    private static string FormatDate(HistoryEntry entry) => entry.Kind switch
    {
        HistoryEntryKind.ConsumptionSummary or HistoryEntryKind.QuoteSummary
            or HistoryEntryKind.TaskSummary or HistoryEntryKind.ReminderSummary
            => entry.Date.ToString("yyyy", CultureInfo.InvariantCulture),
        HistoryEntryKind.Reminder
            => entry.Date.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
        _ => entry.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
    };

    /// <summary>
    /// Texto de la barra de totales (specs/010 §1): el dinero de pedidos y
    /// abonos y el consumido (neto) en clientes —los futuros no pueden tener
    /// pedidos—, y los presupuestos por estado en ambos.
    /// </summary>
    private static string TotalsText(HistoryTotals totals) =>
        totals.OrderCount > 0 || totals.CreditNoteCount > 0
            ? $"{totals.OrderCount} pedido(s) — {SpanishPrice.Format(totals.OrderTotal)} € · " +
                $"{totals.CreditNoteCount} abono(s) — −{SpanishPrice.Format(totals.CreditNoteTotal)} € · " +
                $"Consumido: {HistoryService.FormatNet(totals.Consumed)} € · " +
                $"Presupuestos: {totals.QuoteCount} — {totals.AcceptedQuotes} aceptado(s) · " +
                $"{totals.DismissedQuotes} descartado(s) · {totals.PendingQuotes} pendiente(s)"
            : $"Presupuestos: {totals.QuoteCount} — {totals.AcceptedQuotes} aceptado(s) · " +
                $"{totals.DismissedQuotes} descartado(s) · {totals.PendingQuotes} pendiente(s)";

    /// <summary>
    /// Sello temporal de la base de datos: cambia cuando otro módulo la escribe.
    /// </summary>
    private DateTime GetDatabaseStamp() =>
        File.Exists(_databaseFilePath) ? File.GetLastWriteTimeUtc(_databaseFilePath) : DateTime.MinValue;

    /// <summary>
    /// Al volver a mostrar el módulo, recarga el historial si la base cambió
    /// en disco (algo se guardó en otro módulo). Aquí no hay edición ni
    /// cambios sin guardar: recargar nunca pierde nada.
    /// </summary>
    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);

        if (Visible && _dataStore is not null && _filterId is not null
            && GetDatabaseStamp() != _lastDatabaseStamp)
        {
            LoadHistory();
        }
    }

    /// <summary>
    /// Muestra el error de datos (constitución, punto 5): la tabla queda
    /// vacía hasta la próxima recarga.
    /// </summary>
    private void ReportDataError(Exception ex)
    {
        Prompt(
            "No se pudo leer la base de datos del historial (Data/crm1.db).\n\n" +
            "La tabla se muestra vacía.\n\n" +
            ex.Message,
            "CRM1 — Error de datos",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    /// <summary>
    /// Evita que la tabla lance excepciones con valores ausentes.
    /// </summary>
    private void HistoryDataGridView_DataError(object? sender, System.Windows.Forms.DataGridViewDataErrorEventArgs e)
    {
        e.ThrowException = false;
    }

    /// <summary>
    /// Dibuja el borde grueso alrededor de la fila seleccionada, igual que en
    /// los demás módulos. Las celdas siguen siempre blancas.
    /// </summary>
    private void HistoryDataGridView_RowPostPaint(object? sender, System.Windows.Forms.DataGridViewRowPostPaintEventArgs e)
    {
        if (e.RowIndex < 0 || !historyDataGridView.Rows[e.RowIndex].Selected)
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
