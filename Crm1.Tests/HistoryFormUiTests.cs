using System.Text;
using Crm1.WindowsForms.Forms;
using Crm1.WindowsForms.Models;
using Xunit;
using Task = Crm1.WindowsForms.Models.Task;

namespace Crm1.Tests;

/// <summary>
/// Pruebas de interfaz del módulo de Historial (specs/010): la vista
/// cronológica filtrada al registro con su aviso, contador y barra de
/// totales; el futuro sin pedidos; la recarga al volver a mostrarse y el
/// error visible con base ilegible.
/// </summary>
[Collection("Formularios UI")]
public sealed class HistoryFormUiTests
{
    [Fact]
    public void Client_history_shows_everything_newest_first_with_totals()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);

        Task task = Fixtures.Task(ana.Id);
        task.Action = "Enviar correo";
        task.DueDate = new DateTime(2026, 11, 10);
        task.Description = "propuesta reformada";

        Reminder reminder = Fixtures.Reminder(ana.Id);
        reminder.RemindAt = new DateTime(2026, 11, 20, 10, 30, 0);

        Quote quote = Fixtures.Quote(ana.Id, date: new DateTime(2026, 11, 12),
            amount: 1000m, reference: "PRE-003", description: "Reforma cocina",
            status: QuoteStatus.Accepted);

        Order order = Fixtures.Order(ana.Id, date: new DateTime(2026, 11, 12),
            amount: 1000m, reference: "PRE-003", description: "Reforma cocina");
        Order creditNote = Fixtures.Order(ana.Id, OrderKind.CreditNote,
            date: new DateTime(2026, 11, 20), amount: 150m, reference: "DEV-001",
            description: "");

        UiTestEnvironment.WriteTasks(task);
        UiTestEnvironment.WriteReminders(reminder);
        UiTestEnvironment.WriteQuotes(quote);
        UiTestEnvironment.WriteOrders(order, creditNote);

        WinFormsSta.Run(() =>
        {
            using var form = new HistoryForm(null);
            form.Prompt = new PromptRecorder().Answer;
            form.ShowHistoryOf(TaskRelatedType.Client, ana.Id, "Ana García Pérez");
            DataGridView grid = FormAccess.Grid(form, "historyDataGridView");

            // Alta + recordatorio + abono + pedido + presupuesto + tarea.
            Assert.Equal(6, grid.Rows.Count);
            Assert.Equal("20/11/2026 10:30", FormAccess.CellText(grid.Rows[0], "dateColumn"));
            Assert.Equal("Recordatorio", FormAccess.CellText(grid.Rows[0], "typeColumn"));
            Assert.Equal("Abono", FormAccess.CellText(grid.Rows[1], "typeColumn"));
            Assert.Equal("−150,00 € · DEV-001", FormAccess.CellText(grid.Rows[1], "detailColumn"));
            Assert.Equal("Pedido", FormAccess.CellText(grid.Rows[2], "typeColumn"));
            Assert.Equal("Presupuesto", FormAccess.CellText(grid.Rows[3], "typeColumn"));
            Assert.Equal("1.000,00 € · PRE-003 · Reforma cocina", FormAccess.CellText(grid.Rows[3], "detailColumn"));
            Assert.Equal("Aceptado", FormAccess.CellText(grid.Rows[3], "statusColumn"));
            Assert.Equal("Tarea", FormAccess.CellText(grid.Rows[4], "typeColumn"));
            Assert.Equal("Enviar correo: propuesta reformada", FormAccess.CellText(grid.Rows[4], "detailColumn"));
            Assert.Equal("Alta del cliente", FormAccess.CellText(grid.Rows[5], "typeColumn"));
            Assert.Equal("15/01/2026", FormAccess.CellText(grid.Rows[5], "dateColumn"));

            Assert.Equal("Historial de: (Cliente) Ana García Pérez",
                FormAccess.Label(form, "filterLabel").Text);
            Assert.Equal("Mostrando 6 de 6", FormAccess.Label(form, "counterLabel").Text);

            string totals = FormAccess.Label(form, "totalsLabel").Text;
            Assert.Contains("1 pedido(s) — 1.000,00 €", totals);
            Assert.Contains("1 abono(s) — −150,00 €", totals);
            Assert.Contains("Consumido: 850,00 €", totals);
            Assert.Contains("Presupuestos: 1 — 1 aceptado(s)", totals);
        });
    }

    [Fact]
    public void Prospect_history_shows_only_quotes_in_the_totals()
    {
        Contact clara = Fixtures.Contact("Clara");
        UiTestEnvironment.WriteContacts(clara);

        Task task = Fixtures.Task(clara.Id, TaskRelatedType.Prospect);
        task.DueDate = new DateTime(2026, 11, 10);

        UiTestEnvironment.WriteTasks(task);
        UiTestEnvironment.WriteQuotes(
            Fixtures.Quote(clara.Id, TaskRelatedType.Prospect,
                date: new DateTime(2026, 11, 12), status: QuoteStatus.Accepted),
            Fixtures.Quote(clara.Id, TaskRelatedType.Prospect,
                date: new DateTime(2026, 11, 13), status: QuoteStatus.Dismissed));

        WinFormsSta.Run(() =>
        {
            using var form = new HistoryForm(null);
            form.Prompt = new PromptRecorder().Answer;
            form.ShowHistoryOf(TaskRelatedType.Prospect, clara.Id, "Clara");
            DataGridView grid = FormAccess.Grid(form, "historyDataGridView");

            // Sin fecha de alta en el futuro: presupuesto, presupuesto y tarea.
            Assert.Equal(3, grid.Rows.Count);
            Assert.Equal("Presupuesto", FormAccess.CellText(grid.Rows[0], "typeColumn"));
            Assert.Equal("Presupuesto", FormAccess.CellText(grid.Rows[1], "typeColumn"));
            Assert.Equal("Tarea", FormAccess.CellText(grid.Rows[2], "typeColumn"));

            Assert.Equal("Historial de: (Futuro cliente) Clara",
                FormAccess.Label(form, "filterLabel").Text);
            Assert.Equal("Mostrando 3 de 3", FormAccess.Label(form, "counterLabel").Text);

            // Un futuro no puede tener pedidos: solo la parte de presupuestos.
            Assert.Equal("Presupuestos: 2 — 1 aceptado(s) · 1 descartado(s) · 0 pendiente(s)",
                FormAccess.Label(form, "totalsLabel").Text);
        });
    }

    [Fact]
    public void Showing_the_form_again_reloads_history_changed_on_disk()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteTasks(Fixtures.Task(ana.Id));

        WinFormsSta.Run(() =>
        {
            using var form = new HistoryForm
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            form.Prompt = new PromptRecorder().Answer;
            form.ShowHistoryOf(TaskRelatedType.Client, ana.Id, "Ana García Pérez");
            DataGridView grid = FormAccess.Grid(form, "historyDataGridView");
            Assert.Equal(2, grid.Rows.Count); // alta + tarea

            form.Show();
            form.Visible = false; // como al cambiar a otro módulo

            // Otro módulo guarda una tarea más en la base.
            UiTestEnvironment.WriteTasks(
                Fixtures.Task(ana.Id), Fixtures.Task(ana.Id, action: "Visitar"));

            form.Visible = true; // al volver, la tabla se recarga sola

            Assert.Equal(3, grid.Rows.Count);
            Assert.Equal("Mostrando 3 de 3", FormAccess.Label(form, "counterLabel").Text);
        });
    }

    [Fact]
    public void Corrupt_database_shows_error_and_empty_table()
    {
        UiTestEnvironment.WriteClients(Fixtures.Client("Ana", "12345678Z"));
        File.WriteAllText(UiTestEnvironment.DatabasePath, "esto no es una base de datos", Encoding.UTF8);

        WinFormsSta.Run(() =>
        {
            PromptRecorder recorder = new();

            using var form = new HistoryForm(recorder.Answer)
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            form.ShowHistoryOf(TaskRelatedType.Client,
                Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"), "Ana García Pérez");

            DataGridView grid = FormAccess.Grid(form, "historyDataGridView");
            Assert.Empty(grid.Rows);
            Assert.Equal("Mostrando 0 de 0", FormAccess.Label(form, "counterLabel").Text);
            Assert.Single(recorder.Messages);
            Assert.Contains("No se pudo leer la base de datos del historial", recorder.Messages[0]);
        });
    }
}
