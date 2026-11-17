using System.Text;
using Crm1.WindowsForms.Forms;
using Crm1.WindowsForms.Models;
using Xunit;

namespace Crm1.Tests;

/// <summary>
/// Pruebas de interfaz del módulo de Presupuestos (specs/009): carga
/// descendente, totales y filtro de año, edición, y el flujo completo de
/// aceptación —con conversión en línea del futuro cliente cuando procede—.
/// </summary>
[Collection("Formularios UI")]
public sealed class QuoteFormUiTests
{
    public QuoteFormUiTests()
    {
        UiTestEnvironment.EnsureCatalog();
    }

    // Fechas relativas a hoy para los flujos que guardan o aceptan: la fecha
    // no puede ser anterior a hoy (specs/009 §5).
    private static DateTime Tomorrow => DateTime.Today.AddDays(1);
    private static string TomorrowText => Tomorrow.ToString("dd/MM/yyyy");
    private static string YesterdayText => DateTime.Today.AddDays(-1).ToString("dd/MM/yyyy");

    private static Quote QuoteOf(Guid relatedId, DateTime date,
        TaskRelatedType relatedType = TaskRelatedType.Client,
        QuoteStatus status = QuoteStatus.Pending, Guid? orderId = null,
        decimal amount = 1200.56m) => new()
    {
        Id = Guid.NewGuid(),
        QuoteDate = date,
        Amount = amount,
        Reference = "P-2026-0142",
        Description = "Mobiliario de oficina",
        Status = status,
        RelatedId = relatedId,
        RelatedType = relatedType,
        OrderId = orderId
    };

    [Fact]
    public void Load_shows_quotes_from_newest_to_oldest_with_totals()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteContacts();
        UiTestEnvironment.WriteQuotes(
            QuoteOf(ana.Id, new DateTime(2026, 11, 10)),
            QuoteOf(ana.Id, new DateTime(2026, 11, 15)),
            QuoteOf(ana.Id, new DateTime(2026, 11, 12), status: QuoteStatus.Accepted, orderId: Guid.NewGuid()));

        WinFormsSta.Run(() =>
        {
            using var form = new QuoteForm(new PromptRecorder().Answer);
            DataGridView grid = FormAccess.Grid(form, "quotesDataGridView");

            // Más reciente primero (specs/009 §3) y precio en formato español.
            Assert.Equal(3, grid.Rows.Count);
            Assert.Equal("15/11/2026", FormAccess.CellText(grid.Rows[0], "quoteDateColumn"));
            Assert.Equal("12/11/2026", FormAccess.CellText(grid.Rows[1], "quoteDateColumn"));
            Assert.Equal("1.200,56", FormAccess.CellText(grid.Rows[0], "amountColumn"));
            Assert.Equal("(Cliente) Ana García Pérez", FormAccess.CellText(grid.Rows[0], "relatedColumn"));
            Assert.Equal("Aceptado", FormAccess.CellText(grid.Rows[1], "statusColumn"));
            Assert.Equal("3 presupuesto(s) — 1 aceptado(s)", FormAccess.Label(form, "totalsLabel").Text);
            Assert.Equal("Mostrando 3 de 3", FormAccess.Label(form, "counterLabel").Text);
        });
    }

    [Fact]
    public void Year_filter_shows_only_that_year_and_its_totals()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteContacts();
        UiTestEnvironment.WriteQuotes(
            QuoteOf(ana.Id, new DateTime(2025, 12, 20)),
            QuoteOf(ana.Id, new DateTime(2026, 11, 15)),
            QuoteOf(ana.Id, new DateTime(2026, 1, 5)));

        WinFormsSta.Run(() =>
        {
            using var form = new QuoteForm(new PromptRecorder().Answer);
            DataGridView grid = FormAccess.Grid(form, "quotesDataGridView");
            ComboBox yearFilter = FormAccess.ComboBox(form, "yearFilterComboBox");

            // «Todos» por defecto y los años presentes en los datos.
            Assert.Equal("Todos", yearFilter.Items[0]!.ToString());
            Assert.True(yearFilter.Items.IndexOf("2026") >= 0);
            Assert.Equal("Mostrando 3 de 3", FormAccess.Label(form, "counterLabel").Text);

            yearFilter.SelectedIndex = yearFilter.Items.IndexOf("2025");

            Assert.Equal("Mostrando 1 de 3", FormAccess.Label(form, "counterLabel").Text);
            Assert.Contains("2025: 1 presupuesto(s)", FormAccess.Label(form, "totalsLabel").Text);
            // Descendente: la de 2025 es la última fila; las de 2026 se ocultan.
            Assert.False(grid.Rows[0].Visible);
            Assert.True(grid.Rows[2].Visible);
        });
    }

    [Fact]
    public void Accept_quote_of_a_client_creates_the_order_on_save()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Quote quote = QuoteOf(ana.Id, Tomorrow);
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteContacts();
        UiTestEnvironment.WriteQuotes(quote);
        UiTestEnvironment.WriteOrders();

        WinFormsSta.Run(() =>
        {
            var recorder = new PromptRecorder(DialogResult.Yes, DialogResult.OK, DialogResult.OK);
            using var form = new QuoteForm(recorder.Answer);
            DataGridView grid = FormAccess.Grid(form, "quotesDataGridView");

            grid.CurrentCell = grid.Rows[0].Cells["quoteDateColumn"];
            Assert.Equal("Aceptar", FormAccess.Button(form, "acceptButton").Text);

            FormAccess.Click(form, "AcceptButton_Click");

            // Queda Aceptado en el historial y el botón ya no ofrece nada.
            Assert.Equal("Aceptado", FormAccess.CellText(grid.Rows[0], "statusColumn"));
            Assert.False(FormAccess.Button(form, "acceptButton").Enabled);
            Assert.True(form.HasUnsavedChanges);

            FormAccess.Click(form, "SaveButton_Click");

            Assert.Contains("1 pedido(s)", recorder.Messages[2]);

            // El pedido hereda precio, referencia y descripción, y su fecha
            // es el día de la aceptación (specs/009 §4.3).
            Order order = Assert.Single(UiTestEnvironment.ReadOrders());
            Assert.Equal(ana.Id, order.ClientId);
            Assert.Equal(OrderKind.Order, order.Kind);
            Assert.Equal(DateTime.Today, order.OrderDate);
            Assert.Equal(1200.56m, order.Amount);
            Assert.Equal("P-2026-0142", order.Reference);
            Assert.Equal("Mobiliario de oficina", order.Description);

            Quote saved = Assert.Single(UiTestEnvironment.ReadQuotes());
            Assert.Equal(QuoteStatus.Accepted, saved.Status);
            Assert.Equal(order.Id, saved.OrderId);
            Assert.False(form.HasUnsavedChanges);
        });
    }

    [Fact]
    public void Accept_quote_of_a_prospect_converts_it_and_creates_the_order()
    {
        Contact convertible = Fixtures.ConvertibleContact();
        Quote quote = QuoteOf(convertible.Id, Tomorrow, TaskRelatedType.Prospect);
        Client other = Fixtures.Client("Luis", "23456781R");
        var prospectTask = Fixtures.Task(convertible.Id, TaskRelatedType.Prospect);
        UiTestEnvironment.WriteClients(other);
        UiTestEnvironment.WriteContacts(convertible);
        UiTestEnvironment.WriteQuotes(quote);
        UiTestEnvironment.WriteTasks(prospectTask);
        UiTestEnvironment.WriteOrders();

        WinFormsSta.Run(() =>
        {
            // Aceptar + convertir (oferta y dos confirmaciones) + info + guardar.
            var recorder = new PromptRecorder(
                DialogResult.Yes, DialogResult.Yes, DialogResult.Yes, DialogResult.Yes,
                DialogResult.OK, DialogResult.OK);
            using var form = new QuoteForm(recorder.Answer);
            DataGridView grid = FormAccess.Grid(form, "quotesDataGridView");

            grid.CurrentCell = grid.Rows[0].Cells["quoteDateColumn"];
            FormAccess.Click(form, "AcceptButton_Click");

            Assert.Equal("Aceptado", FormAccess.CellText(grid.Rows[0], "statusColumn"));

            FormAccess.Click(form, "SaveButton_Click");

            Assert.Contains("dado de alta al futuro cliente", recorder.Messages[5]);

            // El futuro desaparece y nace el cliente con el documento del convertido.
            Assert.Empty(UiTestEnvironment.ReadContacts());
            List<Client> clients = UiTestEnvironment.ReadClients();
            Assert.Equal(2, clients.Count);
            Client converted = clients.Single(client => client.TaxId == "11111111H");

            // El presupuesto pasa al cliente nuevo y genera su pedido.
            Quote saved = Assert.Single(UiTestEnvironment.ReadQuotes());
            Assert.Equal(QuoteStatus.Accepted, saved.Status);
            Assert.Equal(converted.Id, saved.RelatedId);
            Assert.Equal(TaskRelatedType.Client, saved.RelatedType);
            Order order = Assert.Single(UiTestEnvironment.ReadOrders());
            Assert.Equal(converted.Id, order.ClientId);
            Assert.Equal(order.Id, saved.OrderId);

            // Las tareas del futuro siguen al cliente nuevo (specs/006).
            var task = Assert.Single(UiTestEnvironment.ReadTasks());
            Assert.Equal(converted.Id, task.RelatedId);
            Assert.Equal(TaskRelatedType.Client, task.RelatedType);
        });
    }

    [Fact]
    public void Accept_quote_of_a_prospect_without_conversion_leaves_it_accepted_without_order()
    {
        Contact convertible = Fixtures.ConvertibleContact();
        Quote quote = QuoteOf(convertible.Id, Tomorrow, TaskRelatedType.Prospect);
        UiTestEnvironment.WriteClients(Fixtures.Client("Luis", "23456781R"));
        UiTestEnvironment.WriteContacts(convertible);
        UiTestEnvironment.WriteQuotes(quote);
        UiTestEnvironment.WriteOrders();

        WinFormsSta.Run(() =>
        {
            var recorder = new PromptRecorder(
                DialogResult.Yes, DialogResult.No, DialogResult.OK, DialogResult.OK);
            using var form = new QuoteForm(recorder.Answer);
            DataGridView grid = FormAccess.Grid(form, "quotesDataGridView");

            grid.CurrentCell = grid.Rows[0].Cells["quoteDateColumn"];
            FormAccess.Click(form, "AcceptButton_Click");
            FormAccess.Click(form, "SaveButton_Click");

            Quote saved = Assert.Single(UiTestEnvironment.ReadQuotes());
            Assert.Equal(QuoteStatus.Accepted, saved.Status);
            Assert.Null(saved.OrderId);
            Assert.Empty(UiTestEnvironment.ReadOrders());
            // El futuro cliente sigue donde estaba: nada se convirtió.
            Assert.Single(UiTestEnvironment.ReadContacts());
        });
    }

    [Fact]
    public void Accept_quote_of_an_incomplete_prospect_shows_the_missing_data()
    {
        // Futuro cliente mínimo: le faltan los datos que Clientes exige.
        Contact incomplete = Fixtures.Contact("Elena");
        Quote quote = QuoteOf(incomplete.Id, Tomorrow, TaskRelatedType.Prospect);
        UiTestEnvironment.WriteClients(Fixtures.Client("Luis", "23456781R"));
        UiTestEnvironment.WriteContacts(incomplete);
        UiTestEnvironment.WriteQuotes(quote);
        UiTestEnvironment.WriteOrders();

        WinFormsSta.Run(() =>
        {
            var recorder = new PromptRecorder(DialogResult.Yes, DialogResult.Yes, DialogResult.OK);
            using var form = new QuoteForm(recorder.Answer);
            DataGridView grid = FormAccess.Grid(form, "quotesDataGridView");

            grid.CurrentCell = grid.Rows[0].Cells["quoteDateColumn"];
            FormAccess.Click(form, "AcceptButton_Click");

            // Carencias visibles y presupuesto Aceptado sin pedido.
            Assert.Contains("no puede convertirse", recorder.Messages[2]);
            Assert.Equal("Aceptado", FormAccess.CellText(grid.Rows[0], "statusColumn"));
            Assert.True(form.HasUnsavedChanges);
            Assert.Empty(UiTestEnvironment.ReadOrders());
        });
    }

    [Fact]
    public void Accepted_quote_without_order_offers_create_order()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Quote quote = QuoteOf(ana.Id, Tomorrow, status: QuoteStatus.Accepted);
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteContacts();
        UiTestEnvironment.WriteQuotes(quote);
        UiTestEnvironment.WriteOrders();

        WinFormsSta.Run(() =>
        {
            var recorder = new PromptRecorder(DialogResult.OK, DialogResult.OK);
            using var form = new QuoteForm(recorder.Answer);
            DataGridView grid = FormAccess.Grid(form, "quotesDataGridView");

            grid.CurrentCell = grid.Rows[0].Cells["quoteDateColumn"];
            Assert.Equal("Crear pedido", FormAccess.Button(form, "acceptButton").Text);
            Assert.True(FormAccess.Button(form, "acceptButton").Enabled);

            FormAccess.Click(form, "AcceptButton_Click");
            FormAccess.Click(form, "SaveButton_Click");

            Quote saved = Assert.Single(UiTestEnvironment.ReadQuotes());
            Assert.NotNull(saved.OrderId);
            Assert.Equal(saved.OrderId, Assert.Single(UiTestEnvironment.ReadOrders()).Id);
        });
    }

    [Fact]
    public void Cancel_without_edition_undoes_the_pending_acceptance()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Quote quote = QuoteOf(ana.Id, Tomorrow);
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteContacts();
        UiTestEnvironment.WriteQuotes(quote);
        UiTestEnvironment.WriteOrders();

        WinFormsSta.Run(() =>
        {
            var recorder = new PromptRecorder(
                DialogResult.Yes, DialogResult.OK, DialogResult.Yes, DialogResult.OK);
            using var form = new QuoteForm(recorder.Answer);
            DataGridView grid = FormAccess.Grid(form, "quotesDataGridView");

            grid.CurrentCell = grid.Rows[0].Cells["quoteDateColumn"];
            FormAccess.Click(form, "AcceptButton_Click");
            Assert.Equal("Aceptado", FormAccess.CellText(grid.Rows[0], "statusColumn"));

            // Cancelar deshace la aceptación (tercera capa de seguridad).
            FormAccess.Click(form, "CancelButton_Click");

            Assert.Equal("Pendiente", FormAccess.CellText(grid.Rows[0], "statusColumn"));
            Assert.False(form.HasUnsavedChanges);

            FormAccess.Click(form, "SaveButton_Click");

            Assert.Equal(QuoteStatus.Pending, Assert.Single(UiTestEnvironment.ReadQuotes()).Status);
            Assert.Empty(UiTestEnvironment.ReadOrders());
        });
    }

    [Fact]
    public void New_quote_saved_persists_all_fields()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteContacts();
        UiTestEnvironment.WriteQuotes();
        UiTestEnvironment.WriteOrders();

        WinFormsSta.Run(() =>
        {
            var recorder = new PromptRecorder(DialogResult.OK);
            using var form = new QuoteForm(recorder.Answer);
            DataGridView grid = FormAccess.Grid(form, "quotesDataGridView");

            FormAccess.Click(form, "NewButton_Click");
            grid.Rows[0].Cells["quoteDateColumn"].Value = TomorrowText;
            grid.Rows[0].Cells["amountColumn"].Value = "1.234,56";
            grid.Rows[0].Cells["referenceColumn"].Value = "P-2026-0200";
            grid.Rows[0].Cells["descriptionColumn"].Value = "Sillas ergonómicas";
            grid.Rows[0].Cells["relatedColumn"].Value = "(Cliente) Ana García Pérez";

            FormAccess.Click(form, "SaveButton_Click");

            Quote saved = Assert.Single(UiTestEnvironment.ReadQuotes());
            Assert.Equal(Tomorrow, saved.QuoteDate);
            Assert.Equal(1234.56m, saved.Amount);
            Assert.Equal("P-2026-0200", saved.Reference);
            Assert.Equal("Sillas ergonómicas", saved.Description);
            Assert.Equal(QuoteStatus.Pending, saved.Status);
            Assert.Equal(ana.Id, saved.RelatedId);
        });
    }

    [Fact]
    public void Modify_then_cancel_restores_and_save_persists_the_change()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Quote quote = QuoteOf(ana.Id, Tomorrow);
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteContacts();
        UiTestEnvironment.WriteQuotes(quote);
        UiTestEnvironment.WriteOrders();

        WinFormsSta.Run(() =>
        {
            var recorder = new PromptRecorder(DialogResult.OK);
            using var form = new QuoteForm(recorder.Answer);
            DataGridView grid = FormAccess.Grid(form, "quotesDataGridView");

            grid.CurrentCell = grid.Rows[0].Cells["quoteDateColumn"];
            FormAccess.Click(form, "ModifyButton_Click");
            grid.Rows[0].Cells["amountColumn"].Value = "999,99";
            FormAccess.Click(form, "CancelButton_Click");
            Assert.Equal("1.200,56", FormAccess.CellText(grid.Rows[0], "amountColumn"));
            Assert.False(form.HasUnsavedChanges);

            grid.CurrentCell = grid.Rows[0].Cells["quoteDateColumn"];
            FormAccess.Click(form, "ModifyButton_Click");
            grid.Rows[0].Cells["amountColumn"].Value = "999,99";
            FormAccess.Click(form, "SaveButton_Click");

            Assert.Equal(999.99m, Assert.Single(UiTestEnvironment.ReadQuotes()).Amount);
        });
    }

    [Fact]
    public void Saving_with_a_past_date_shows_the_message_and_keeping_the_edit()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteContacts();
        UiTestEnvironment.WriteQuotes();
        UiTestEnvironment.WriteOrders();

        WinFormsSta.Run(() =>
        {
            var recorder = new PromptRecorder(DialogResult.OK, DialogResult.OK);
            using var form = new QuoteForm(recorder.Answer);
            DataGridView grid = FormAccess.Grid(form, "quotesDataGridView");

            FormAccess.Click(form, "NewButton_Click");
            grid.Rows[0].Cells["quoteDateColumn"].Value = YesterdayText;
            grid.Rows[0].Cells["amountColumn"].Value = "1.000,50";
            grid.Rows[0].Cells["relatedColumn"].Value = "(Cliente) Ana García Pérez";

            FormAccess.Click(form, "SaveButton_Click");

            // Mensaje claro, celda marcada, edición abierta y nada escrito.
            Assert.Single(recorder.Messages);
            Assert.Equal("La fecha no puede ser anterior a hoy.", recorder.Messages[0]);
            Assert.True(form.IsEditing);
            Assert.True(form.HasUnsavedChanges);
            Assert.Empty(UiTestEnvironment.ReadQuotes());

            // Al corregir la fecha, guarda con normalidad.
            grid.Rows[0].Cells["quoteDateColumn"].Value = TomorrowText;
            FormAccess.Click(form, "SaveButton_Click");

            Quote saved = Assert.Single(UiTestEnvironment.ReadQuotes());
            Assert.Equal(Tomorrow, saved.QuoteDate);
            Assert.False(form.IsEditing);
        });
    }

    [Fact]
    public void Saving_keeps_unchanged_historical_rows_without_revalidating_their_date()
    {
        // Historial legítimo con fecha pasada: guardar sin tocarlo lo conserva.
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Quote historical = QuoteOf(ana.Id, new DateTime(2025, 12, 20));
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteContacts();
        UiTestEnvironment.WriteQuotes(historical);
        UiTestEnvironment.WriteOrders();

        WinFormsSta.Run(() =>
        {
            var recorder = new PromptRecorder(DialogResult.OK);
            using var form = new QuoteForm(recorder.Answer);
            DataGridView grid = FormAccess.Grid(form, "quotesDataGridView");
            Assert.Equal("20/12/2025", FormAccess.CellText(grid.Rows[0], "quoteDateColumn"));

            FormAccess.Click(form, "SaveButton_Click");

            Assert.Contains("1 presupuesto(s)", recorder.Messages[0]);
            Quote saved = Assert.Single(UiTestEnvironment.ReadQuotes());
            Assert.Equal(new DateTime(2025, 12, 20), saved.QuoteDate);
            Assert.False(form.HasUnsavedChanges);
        });
    }

    [Fact]
    public void Accept_pending_quote_with_an_old_date_creates_an_order_dated_today()
    {
        // Pendiente con 20 días (fecha pasada pero dentro de la validez):
        // debe poder aceptarse y su pedido nacer con fecha de hoy.
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Quote oldPending = QuoteOf(ana.Id, DateTime.Today.AddDays(-20));
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteContacts();
        UiTestEnvironment.WriteQuotes(oldPending);
        UiTestEnvironment.WriteOrders();

        WinFormsSta.Run(() =>
        {
            var recorder = new PromptRecorder(DialogResult.Yes, DialogResult.OK, DialogResult.OK);
            using var form = new QuoteForm(recorder.Answer);
            DataGridView grid = FormAccess.Grid(form, "quotesDataGridView");

            grid.CurrentCell = grid.Rows[0].Cells["quoteDateColumn"];
            FormAccess.Click(form, "AcceptButton_Click");
            FormAccess.Click(form, "SaveButton_Click");

            Quote saved = Assert.Single(UiTestEnvironment.ReadQuotes());
            Assert.Equal(QuoteStatus.Accepted, saved.Status);

            Order order = Assert.Single(UiTestEnvironment.ReadOrders());
            Assert.Equal(DateTime.Today, order.OrderDate);
            Assert.Equal(oldPending.Amount, order.Amount);
            Assert.Equal(oldPending.Reference, order.Reference);
            Assert.Equal(oldPending.Description, order.Description);
        });
    }

    [Fact]
    public void Modifying_a_historical_quote_to_accepted_is_not_blocked_by_its_old_date()
    {
        // Cambio manual del Estado en la tabla: la fecha antigua no bloquea.
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Quote oldPending = QuoteOf(ana.Id, DateTime.Today.AddDays(-25));
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteContacts();
        UiTestEnvironment.WriteQuotes(oldPending);
        UiTestEnvironment.WriteOrders();

        WinFormsSta.Run(() =>
        {
            var recorder = new PromptRecorder(DialogResult.OK);
            using var form = new QuoteForm(recorder.Answer);
            DataGridView grid = FormAccess.Grid(form, "quotesDataGridView");

            grid.CurrentCell = grid.Rows[0].Cells["quoteDateColumn"];
            FormAccess.Click(form, "ModifyButton_Click");
            grid.Rows[0].Cells["statusColumn"].Value = "Aceptado";

            FormAccess.Click(form, "SaveButton_Click");

            Quote saved = Assert.Single(UiTestEnvironment.ReadQuotes());
            Assert.Equal(QuoteStatus.Accepted, saved.Status);
            Assert.Equal(oldPending.QuoteDate, saved.QuoteDate);
        });
    }

    [Fact]
    public void ShowQuotesOf_filters_the_view_and_saving_keeps_hidden_quotes()    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Client luis = Fixtures.Client("Luis", "23456781R");
        Quote anaQuote = QuoteOf(ana.Id, Tomorrow);
        Quote luisQuote = QuoteOf(luis.Id, new DateTime(2026, 11, 10));
        UiTestEnvironment.WriteClients(ana, luis);
        UiTestEnvironment.WriteContacts();
        UiTestEnvironment.WriteQuotes(anaQuote, luisQuote);
        UiTestEnvironment.WriteOrders();

        WinFormsSta.Run(() =>
        {
            var recorder = new PromptRecorder(DialogResult.OK);
            using var form = new QuoteForm(recorder.Answer);
            DataGridView grid = FormAccess.Grid(form, "quotesDataGridView");

            form.ShowQuotesOf(TaskRelatedType.Client, ana.Id, "Ana García Pérez");

            Assert.Single(grid.Rows);
            Assert.Equal("Mostrando 1 de 1", FormAccess.Label(form, "counterLabel").Text);
            Assert.Contains("Presupuestos de: (Cliente) Ana García Pérez",
                FormAccess.Label(form, "filterLabel").Text);

            // El filtro nunca hace perder datos: Guardar conserva las ocultas.
            FormAccess.Click(form, "SaveButton_Click");
            Assert.Equal(2, UiTestEnvironment.ReadQuotes().Count);

            form.ShowAllQuotes();
            Assert.Equal(2, grid.Rows.Count);
        });
    }
}
