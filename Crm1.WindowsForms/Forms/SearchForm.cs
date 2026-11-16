using System.Globalization;
using Crm1.WindowsForms.Models;
using Crm1.WindowsForms.Services;

namespace Crm1.WindowsForms.Forms;

/// <summary>
/// Formulario de búsqueda global (specs/004-busqueda-global.md): busca en los
/// clientes y futuros clientes guardados en disco y navega hasta el registro
/// elegido. La lógica de coincidencia vive en SearchService (constitución, punto 3).
/// </summary>
public partial class SearchForm : System.Windows.Forms.Form
{
    // Color y grosor del borde que marca la fila seleccionada (igual que los módulos).
    private static readonly System.Drawing.Color SelectionBorderColor = System.Drawing.Color.FromArgb(100, 116, 139);
    private const float SelectionBorderWidth = 3F;

    // Lectura de la base de datos; la búsqueda nunca escribe en disco. En
    // concreto SqlDataStore porque también carga tareas y recordatorios
    // (specs/005 y specs/007), que viven fuera de IDataStore.
    private readonly SqlDataStore _dataStore = new();

    /// <summary>
    /// Se lanza al elegir un resultado (doble clic o Enter) para pedir a
    /// MainForm que muestre el módulo correspondiente y marque el registro.
    /// </summary>
    public event Action<SearchModule, Guid>? NavigateRequested;

    public SearchForm()
    {
        // Inicializa los componentes definidos en el diseñador.
        InitializeComponent();
    }

    /// <summary>
    /// Da el foco al cuadro de búsqueda cada vez que el módulo se muestra.
    /// </summary>
    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (Visible)
        {
            searchTextBox.Focus();
        }
    }

    /// <summary>
    /// Repite la búsqueda en vivo con cada cambio del texto.
    /// </summary>
    private void SearchTextBox_TextChanged(object? sender, EventArgs e)
    {
        RunSearch();
    }

    /// <summary>
    /// Busca en los datos guardados y rellena las dos tablas de resultados:
    /// clientes y futuros clientes arriba (sus 13 columnas) y tareas y
    /// recordatorios abajo (specs/004 §7). Un fallo de lectura se muestra
    /// como error y deja los resultados vacíos, nunca como una búsqueda
    /// correcta sin filas (constitución, punto 5).
    /// </summary>
    private void RunSearch()
    {
        resultsDataGridView.Rows.Clear();
        tasksDataGridView.Rows.Clear();

        string term = searchTextBox.Text;
        if (term.Trim().Length == 0)
        {
            tasksLabel.Visible = false;
            tasksDataGridView.Visible = false;
            return;
        }

        List<Client> clients;
        List<Contact> contacts;
        List<Models.Task> tasks;
        List<Reminder> reminders;
        try
        {
            clients = _dataStore.LoadClients();
            contacts = _dataStore.LoadContacts();
            tasks = _dataStore.LoadTasks();
            reminders = _dataStore.LoadReminders();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "No se pudo leer la base de datos (Data/crm1.db).\n\n" +
                ex.Message,
                "CRM1 — Error de datos",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        foreach (SearchMatch match in SearchService.Search(clients, contacts, tasks, reminders, term))
        {
            if (match.Module is SearchModule.Task or SearchModule.Reminder)
            {
                AddTaskResultRow(match);
            }
            else
            {
                AddRecordResultRow(match);
            }
        }

        // La tabla de tareas y recordatorios solo ocupa espacio si hay filas.
        bool hasTaskRows = tasksDataGridView.Rows.Count > 0;
        tasksLabel.Visible = hasTaskRows;
        tasksDataGridView.Visible = hasTaskRows;
    }

    /// <summary>
    /// Añade un resultado de cliente o futuro cliente a la tabla superior,
    /// con el registro completo igual que la tabla de su módulo.
    /// </summary>
    private void AddRecordResultRow(SearchMatch match)
    {
        int index;
        if (match.Module == SearchModule.Client)
        {
            Client client = match.Client!;
            index = resultsDataGridView.Rows.Add(
                "Clientes",
                client.FirstName,
                client.LastName1,
                client.LastName2,
                client.TaxId,
                client.Phone,
                client.Email,
                client.Address,
                client.PostalCode,
                client.City,
                client.Province,
                client.Status == ClientStatus.Inactive ? "Inactivo" : "Activo",
                client.RegistrationDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                client.Notes);
        }
        else
        {
            Contact contact = match.Contact!;
            index = resultsDataGridView.Rows.Add(
                "Futuros clientes",
                contact.FirstName,
                contact.LastName1,
                contact.LastName2,
                contact.TaxId,
                contact.Phone,
                contact.Email,
                contact.Address,
                contact.PostalCode,
                contact.City,
                contact.Province,
                contact.Status == ContactStatus.Discarded ? "Descartado" : "Pendiente",
                contact.RegistrationDate == default
                    ? string.Empty
                    : contact.RegistrationDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                contact.Notes);
        }

        resultsDataGridView.Rows[index].Tag = (match.Module, match.Id);
    }

    /// <summary>
    /// Añade un resultado de tarea o recordatorio a la tabla inferior: tipo,
    /// título o asunto, registro vinculado con su prefijo, fecha y estado.
    /// </summary>
    private void AddTaskResultRow(SearchMatch match)
    {
        int index;
        if (match.Module == SearchModule.Task)
        {
            Models.Task task = match.Task!;
            index = tasksDataGridView.Rows.Add(
                "Tarea",
                task.Action,
                match.RelatedName,
                task.DueDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                task.Status switch
                {
                    Models.TaskStatus.Completed => "Completada",
                    Models.TaskStatus.InProgress => "En curso",
                    _ => "Pendiente"
                });
        }
        else
        {
            Reminder reminder = match.Reminder!;
            index = tasksDataGridView.Rows.Add(
                "Recordatorio",
                reminder.Subject,
                match.RelatedName,
                reminder.RemindAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                reminder.Status == ReminderStatus.Dismissed ? "Descartado" : "Activo");
        }

        tasksDataGridView.Rows[index].Tag = (match.Module, match.Id);
    }

    /// <summary>
    /// Dibuja el borde grueso alrededor de la fila seleccionada, la misma marca
    /// visual que en los módulos; las celdas siguen siempre blancas. Sirve a
    /// las dos tablas de resultados.
    /// </summary>
    private void ResultsDataGridView_RowPostPaint(object? sender, System.Windows.Forms.DataGridViewRowPostPaintEventArgs e)
    {
        if (e.RowIndex < 0 || sender is not DataGridView grid || !grid.Rows[e.RowIndex].Selected)
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
    /// Navega hasta el registro de la fila con doble clic (ambas tablas).
    /// </summary>
    private void ResultsDataGridView_CellDoubleClick(object? sender, System.Windows.Forms.DataGridViewCellEventArgs e)
    {
        NavigateToResult(sender as DataGridView, e.RowIndex);
    }

    /// <summary>
    /// Navega con Enter a la fila actual; la tecla se consume para que no
    /// avance a la fila siguiente (ambas tablas).
    /// </summary>
    private void ResultsDataGridView_KeyDown(object? sender, System.Windows.Forms.KeyEventArgs e)
    {
        if (e.KeyCode == System.Windows.Forms.Keys.Enter
            && sender is DataGridView { CurrentCell: not null } grid)
        {
            e.Handled = true;
            NavigateToResult(grid, grid.CurrentCell.RowIndex);
        }
    }

    /// <summary>
    /// Pide a MainForm que muestre el módulo y marque el registro encontrado.
    /// </summary>
    private void NavigateToResult(DataGridView? grid, int rowIndex)
    {
        if (grid is null || rowIndex < 0)
        {
            return;
        }

        if (grid.Rows[rowIndex].Tag is (SearchModule module, Guid id))
        {
            NavigateRequested?.Invoke(module, id);
        }
    }
}
