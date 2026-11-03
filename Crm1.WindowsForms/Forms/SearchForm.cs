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

    // Lectura de los archivos de datos; la búsqueda nunca escribe en disco.
    private readonly IDataStore _dataStore = new JsonDataStore();

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
    /// Busca en los datos guardados y rellena la tabla de resultados. Un fallo
    /// de lectura se muestra como error y deja los resultados vacíos, nunca
    /// como una búsqueda correcta sin filas (constitución, punto 5).
    /// </summary>
    private void RunSearch()
    {
        resultsDataGridView.Rows.Clear();

        string term = searchTextBox.Text;
        if (term.Trim().Length == 0)
        {
            return;
        }

        List<Client> clients;
        List<Contact> contacts;
        try
        {
            clients = _dataStore.LoadClients();
            contacts = _dataStore.LoadContacts();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "No se pudieron leer los archivos de datos (Data/clients.json y Data/contacts.json).\n\n" +
                ex.Message,
                "CRM1 — Error de datos",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        foreach (SearchMatch match in SearchService.Search(clients, contacts, term))
        {
            // Rellena la fila con el registro completo, igual que la tabla de su módulo.
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
    }

    /// <summary>
    /// Dibuja el borde grueso alrededor de la fila seleccionada, la misma marca
    /// visual que en ClientForm y ContactForm; las celdas siguen siempre blancas.
    /// </summary>
    private void ResultsDataGridView_RowPostPaint(object? sender, System.Windows.Forms.DataGridViewRowPostPaintEventArgs e)
    {
        if (e.RowIndex < 0 || !resultsDataGridView.Rows[e.RowIndex].Selected)
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
    /// Navega hasta el registro de la fila con doble clic.
    /// </summary>
    private void ResultsDataGridView_CellDoubleClick(object? sender, System.Windows.Forms.DataGridViewCellEventArgs e)
    {
        NavigateToResult(e.RowIndex);
    }

    /// <summary>
    /// Navega con Enter a la fila actual; la tecla se consume para que no
    /// avance a la fila siguiente.
    /// </summary>
    private void ResultsDataGridView_KeyDown(object? sender, System.Windows.Forms.KeyEventArgs e)
    {
        if (e.KeyCode == System.Windows.Forms.Keys.Enter && resultsDataGridView.CurrentCell is not null)
        {
            e.Handled = true;
            NavigateToResult(resultsDataGridView.CurrentCell.RowIndex);
        }
    }

    /// <summary>
    /// Pide a MainForm que muestre el módulo y marque el registro encontrado.
    /// </summary>
    private void NavigateToResult(int rowIndex)
    {
        if (rowIndex < 0)
        {
            return;
        }

        if (resultsDataGridView.Rows[rowIndex].Tag is (SearchModule module, Guid id))
        {
            NavigateRequested?.Invoke(module, id);
        }
    }
}
