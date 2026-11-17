using Crm1.WindowsForms.Forms;
using Crm1.WindowsForms.Models;
using Xunit;

namespace Crm1.Tests;

/// <summary>
/// Pruebas de interfaz del módulo de Pedidos y abonos (specs/009): carga
/// descendente, tipos, totales netos con abonos, filtro de año, edición y
/// vistas filtradas por cliente con conservación de los ocultos.
/// </summary>
[Collection("Formularios UI")]
public sealed class OrderFormUiTests
{
    public OrderFormUiTests()
    {
        UiTestEnvironment.EnsureCatalog();
    }

    private static Order OrderOf(Guid clientId, DateTime date,
        OrderKind kind = OrderKind.Order, decimal amount = 1000m,
        string description = "Mobiliario de oficina") => new()
    {
        Id = Guid.NewGuid(),
        Kind = kind,
        OrderDate = date,
        Amount = amount,
        Reference = "E-2026-0099",
        Description = description,
        ClientId = clientId
    };

    [Fact]
    public void Load_shows_orders_from_newest_to_oldest_with_net_totals()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteContacts();
        UiTestEnvironment.WriteOrders(
            OrderOf(ana.Id, new DateTime(2026, 11, 10), amount: 1000m),
            OrderOf(ana.Id, new DateTime(2026, 11, 15), amount: 500.50m),
            OrderOf(ana.Id, new DateTime(2026, 11, 12), OrderKind.CreditNote, 150m, "Devolución sillas"));

        WinFormsSta.Run(() =>
        {
            using var form = new OrderForm(new PromptRecorder().Answer);
            DataGridView grid = FormAccess.Grid(form, "ordersDataGridView");

            Assert.Equal(3, grid.Rows.Count);
            Assert.Equal("15/11/2026", FormAccess.CellText(grid.Rows[0], "orderDateColumn"));
            Assert.Equal("Pedido", FormAccess.CellText(grid.Rows[0], "kindColumn"));
            Assert.Equal("Abono", FormAccess.CellText(grid.Rows[1], "kindColumn"));
            Assert.Equal("(Cliente) Ana García Pérez", FormAccess.CellText(grid.Rows[0], "clientColumn"));

            Assert.Equal(
                "2 pedido(s) — 1.500,50 € · 1 abono(s) — −150,00 € · Neto: 1.350,50 €",
                FormAccess.Label(form, "totalsLabel").Text);
            Assert.Equal("Mostrando 3 de 3", FormAccess.Label(form, "counterLabel").Text);
        });
    }

    [Fact]
    public void Year_filter_shows_only_that_year()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteContacts();
        UiTestEnvironment.WriteOrders(
            OrderOf(ana.Id, new DateTime(2025, 12, 20)),
            OrderOf(ana.Id, new DateTime(2026, 11, 15)));

        WinFormsSta.Run(() =>
        {
            using var form = new OrderForm(new PromptRecorder().Answer);
            ComboBox yearFilter = FormAccess.ComboBox(form, "yearFilterComboBox");

            Assert.Equal("Todos", yearFilter.Items[0]!.ToString());
            yearFilter.SelectedIndex = yearFilter.Items.IndexOf("2025");

            Assert.Equal("Mostrando 1 de 2", FormAccess.Label(form, "counterLabel").Text);
            Assert.StartsWith("2025: 1 pedido(s)", FormAccess.Label(form, "totalsLabel").Text);
        });
    }

    [Fact]
    public void New_order_saved_directly_persists()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteContacts();
        UiTestEnvironment.WriteOrders();

        WinFormsSta.Run(() =>
        {
            var recorder = new PromptRecorder(DialogResult.OK);
            using var form = new OrderForm(recorder.Answer);
            DataGridView grid = FormAccess.Grid(form, "ordersDataGridView");

            FormAccess.Click(form, "NewButton_Click");
            Assert.Equal("Pedido", FormAccess.CellText(grid.Rows[0], "kindColumn"));
            grid.Rows[0].Cells["orderDateColumn"].Value = "12/11/2026";
            grid.Rows[0].Cells["amountColumn"].Value = "980,75";
            grid.Rows[0].Cells["clientColumn"].Value = "(Cliente) Ana García Pérez";

            FormAccess.Click(form, "SaveButton_Click");

            Order saved = Assert.Single(UiTestEnvironment.ReadOrders());
            Assert.Equal(OrderKind.Order, saved.Kind);
            Assert.Equal(new DateTime(2026, 11, 12), saved.OrderDate);
            Assert.Equal(980.75m, saved.Amount);
            Assert.Equal(ana.Id, saved.ClientId);
        });
    }

    [Fact]
    public void New_credit_note_subtracts_from_the_net_total()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteContacts();
        UiTestEnvironment.WriteOrders(
            OrderOf(ana.Id, new DateTime(2026, 11, 10), amount: 1000m),
            OrderOf(ana.Id, new DateTime(2026, 11, 15), amount: 500.50m));

        WinFormsSta.Run(() =>
        {
            var recorder = new PromptRecorder(DialogResult.OK);
            using var form = new OrderForm(recorder.Answer);
            DataGridView grid = FormAccess.Grid(form, "ordersDataGridView");

            FormAccess.Click(form, "NewButton_Click");
            grid.Rows[0].Cells["kindColumn"].Value = "Abono";
            grid.Rows[0].Cells["orderDateColumn"].Value = "16/11/2026";
            grid.Rows[0].Cells["amountColumn"].Value = "150";
            grid.Rows[0].Cells["clientColumn"].Value = "(Cliente) Ana García Pérez";

            FormAccess.Click(form, "SaveButton_Click");

            Assert.Equal(
                "2 pedido(s) — 1.500,50 € · 1 abono(s) — −150,00 € · Neto: 1.350,50 €",
                FormAccess.Label(form, "totalsLabel").Text);
            Assert.Equal(3, UiTestEnvironment.ReadOrders().Count);
        });
    }

    [Fact]
    public void Invalid_price_is_rejected_with_a_short_message()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteContacts();
        UiTestEnvironment.WriteOrders();

        WinFormsSta.Run(() =>
        {
            var recorder = new PromptRecorder(DialogResult.OK);
            using var form = new OrderForm(recorder.Answer);
            DataGridView grid = FormAccess.Grid(form, "ordersDataGridView");

            FormAccess.Click(form, "NewButton_Click");
            grid.Rows[0].Cells["orderDateColumn"].Value = "12/11/2026";
            grid.Rows[0].Cells["amountColumn"].Value = "12,3,4";
            grid.Rows[0].Cells["clientColumn"].Value = "(Cliente) Ana García Pérez";

            FormAccess.Click(form, "SaveButton_Click");

            Assert.Single(recorder.Messages);
            Assert.Contains("precio no es válido", recorder.Messages[0]);
            Assert.True(form.IsEditing); // la edición sigue abierta con errores
            Assert.Empty(UiTestEnvironment.ReadOrders());
        });
    }

    [Fact]
    public void ShowOrdersOf_filters_by_client_and_saving_keeps_hidden_orders()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Client luis = Fixtures.Client("Luis", "23456781R");
        UiTestEnvironment.WriteClients(ana, luis);
        UiTestEnvironment.WriteContacts();
        UiTestEnvironment.WriteOrders(
            OrderOf(ana.Id, new DateTime(2026, 11, 15), amount: 1000m),
            OrderOf(luis.Id, new DateTime(2026, 11, 10), amount: 250m));

        WinFormsSta.Run(() =>
        {
            var recorder = new PromptRecorder(DialogResult.OK);
            using var form = new OrderForm(recorder.Answer);
            DataGridView grid = FormAccess.Grid(form, "ordersDataGridView");

            form.ShowOrdersOf(ana.Id, "Ana García Pérez");

            Assert.Single(grid.Rows);
            Assert.Equal("Mostrando 1 de 1", FormAccess.Label(form, "counterLabel").Text);
            Assert.Equal(
                "1 pedido(s) — 1.000,00 € · 0 abono(s) — −0,00 € · Neto: 1.000,00 €",
                FormAccess.Label(form, "totalsLabel").Text);

            FormAccess.Click(form, "SaveButton_Click");
            Assert.Equal(2, UiTestEnvironment.ReadOrders().Count);

            form.ShowAllOrders();
            Assert.Equal(2, grid.Rows.Count);
        });
    }

    [Fact]
    public void SelectOrderById_marks_the_order_and_rejects_unknown_ids()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Order first = OrderOf(ana.Id, new DateTime(2026, 11, 10));
        Order second = OrderOf(ana.Id, new DateTime(2026, 11, 15));
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteContacts();
        UiTestEnvironment.WriteOrders(first, second);

        WinFormsSta.Run(() =>
        {
            using var form = new OrderForm(new PromptRecorder().Answer);

            Assert.False(form.IsEditing);
            Assert.True(form.SelectOrderById(second.Id));
            Assert.Equal(second.Id, form.CurrentOrderId);
            Assert.False(form.SelectOrderById(Guid.NewGuid()));
        });
    }
}
