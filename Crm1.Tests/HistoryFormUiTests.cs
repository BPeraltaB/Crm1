using System.Text;
using Crm1.WindowsForms.Forms;
using Crm1.WindowsForms.Models;
using Xunit;
using Task = Crm1.WindowsForms.Models.Task;
using TaskStatus = Crm1.WindowsForms.Models.TaskStatus;

namespace Crm1.Tests;

/// <summary>
/// Pruebas de interfaz del módulo de Historial (specs/010): la vista
/// cronológica filtrada al registro con su aviso, contador y barra de
/// totales — detalle solo de lo que falta y resúmenes por año —; la recarga
/// al volver a mostrarse y el error visible con base ilegible.
/// </summary>
[Collection("Formularios UI")]
public sealed class HistoryFormUiTests
{
    [Fact]
    public void Client_history_shows_summaries_by_year_and_only_the_outstanding()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);

        Task pending = Fixtures.Task(ana.Id);
        pending.Action = "Enviar correo";
        pending.DueDate = new DateTime(2026, 11, 10);
        pending.Description = "propuesta reformada";

        Task completed = Fixtures.Task(ana.Id);
        completed.Action = "Llamar";
        completed.DueDate = new DateTime(2026, 3, 5);
        completed.Status = TaskStatus.Completed;

        Reminder active = Fixtures.Reminder(ana.Id);
        active.RemindAt = new DateTime(2026, 11, 20, 10, 30, 0);

        Reminder dismissed = Fixtures.Reminder(ana.Id);
        dismissed.RemindAt = new DateTime(2025, 5, 2, 9, 0, 0);
        dismissed.Status = ReminderStatus.Dismissed;

        Quote quote = Fixtures.Quote(ana.Id, date: new DateTime(2026, 11, 12),
            amount: 1000m, reference: "PRE-003", description: "Reforma cocina",
            status: QuoteStatus.Accepted);

        Order order = Fixtures.Order(ana.Id, date: new DateTime(2026, 11, 12),
            amount: 1000m, reference: "PRE-003", description: "Reforma cocina");
        Order creditNote = Fixtures.Order(ana.Id, OrderKind.CreditNote,
            date: new DateTime(2026, 11, 20), amount: 150m, reference: "DEV-001",
            description: "");

        UiTestEnvironment.WriteTasks(pending, completed);
        UiTestEnvironment.WriteReminders(active, dismissed);
        UiTestEnvironment.WriteQuotes(quote);
        UiTestEnvironment.WriteOrders(order, creditNote);

        WinFormsSta.Run(() =>
        {
            using var form = new HistoryForm(null);
            form.Prompt = new PromptRecorder().Answer;
            form.ShowHistoryOf(TaskRelatedType.Client, ana.Id, "Ana García Pérez");
            DataGridView grid = FormAccess.Grid(form, "historyDataGridView");

            // Bloques por año con sus resúmenes y, entre medias, lo que falta.
            Assert.Equal(7, grid.Rows.Count);
            Assert.Equal("2026", FormAccess.CellText(grid.Rows[0], "dateColumn"));
            Assert.Equal("Consumo 2026", FormAccess.CellText(grid.Rows[0], "typeColumn"));
            Assert.Equal("1 pedido — 1.000,00 € · 1 abono — −150,00 € · Consumido: 850,00 €",
                FormAccess.CellText(grid.Rows[0], "detailColumn"));
            Assert.Equal("Presupuestos 2026", FormAccess.CellText(grid.Rows[1], "typeColumn"));
            Assert.Equal("1 — 1 aceptado", FormAccess.CellText(grid.Rows[1], "detailColumn"));
            Assert.Equal("Tareas 2026", FormAccess.CellText(grid.Rows[2], "typeColumn"));
            Assert.Equal("1 completada", FormAccess.CellText(grid.Rows[2], "detailColumn"));

            Assert.Equal("20/11/2026 10:30", FormAccess.CellText(grid.Rows[3], "dateColumn"));
            Assert.Equal("Recordatorio", FormAccess.CellText(grid.Rows[3], "typeColumn"));
            Assert.Equal("Activo", FormAccess.CellText(grid.Rows[3], "statusColumn"));
            Assert.Equal("10/11/2026", FormAccess.CellText(grid.Rows[4], "dateColumn"));
            Assert.Equal("Tarea", FormAccess.CellText(grid.Rows[4], "typeColumn"));
            Assert.Equal("Enviar correo: propuesta reformada", FormAccess.CellText(grid.Rows[4], "detailColumn"));
            Assert.Equal("Pendiente", FormAccess.CellText(grid.Rows[4], "statusColumn"));

            Assert.Equal("15/01/2026", FormAccess.CellText(grid.Rows[5], "dateColumn"));
            Assert.Equal("Alta del cliente", FormAccess.CellText(grid.Rows[5], "typeColumn"));
            Assert.Equal("2025", FormAccess.CellText(grid.Rows[6], "dateColumn"));
            Assert.Equal("Recordatorios 2025", FormAccess.CellText(grid.Rows[6], "typeColumn"));
            Assert.Equal("1 descartado", FormAccess.CellText(grid.Rows[6], "detailColumn"));

            Assert.Equal("Historial de: (Cliente) Ana García Pérez",
                FormAccess.Label(form, "filterLabel").Text);
            Assert.Equal("Mostrando 7 de 7", FormAccess.Label(form, "counterLabel").Text);

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

        Task pending = Fixtures.Task(clara.Id, TaskRelatedType.Prospect);
        pending.DueDate = new DateTime(2026, 11, 10);

        Task completed = Fixtures.Task(clara.Id, TaskRelatedType.Prospect);
        completed.DueDate = new DateTime(2026, 3, 2);
        completed.Status = TaskStatus.Completed;

        UiTestEnvironment.WriteTasks(pending, completed);
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

            // Sin fecha de alta en el futuro: presupuestos, tareas y detalle.
            Assert.Equal(3, grid.Rows.Count);
            Assert.Equal("2026", FormAccess.CellText(grid.Rows[0], "dateColumn"));
            Assert.Equal("Presupuestos 2026", FormAccess.CellText(grid.Rows[0], "typeColumn"));
            Assert.Equal("2 — 1 aceptado · 1 descartado", FormAccess.CellText(grid.Rows[0], "detailColumn"));
            Assert.Equal("Tareas 2026", FormAccess.CellText(grid.Rows[1], "typeColumn"));
            Assert.Equal("1 completada", FormAccess.CellText(grid.Rows[1], "detailColumn"));
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
        UiTestEnvironment.WriteTasks(Fixtures.Task(ana.Id), Fixtures.Task(ana.Id, action: "Visitar"));

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
            Assert.Equal(3, grid.Rows.Count); // alta + 2 tareas pendientes

            form.Show();
            form.Visible = false; // como al cambiar a otro módulo

            // Otro módulo guarda una tarea más en la base.
            UiTestEnvironment.WriteTasks(
                Fixtures.Task(ana.Id), Fixtures.Task(ana.Id, action: "Visitar"),
                Fixtures.Task(ana.Id, action: "Reunión"));

            form.Visible = true; // al volver, la tabla se recarga sola

            Assert.Equal(4, grid.Rows.Count);
            Assert.Equal("Mostrando 4 de 4", FormAccess.Label(form, "counterLabel").Text);
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

    // Navegación con doble clic (specs/010 §6): la línea detallada lleva su
    // registro y la de alta, el tipo sin identificador.
    [Fact]
    public void Double_click_requests_navigation_with_kind_and_target_id()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        UiTestEnvironment.WriteClients(ana);

        Reminder active = Fixtures.Reminder(ana.Id);
        active.RemindAt = new DateTime(2026, 11, 20, 10, 30, 0);
        UiTestEnvironment.WriteReminders(active);
        UiTestEnvironment.WriteTasks(Fixtures.Task(ana.Id));

        WinFormsSta.Run(() =>
        {
            using var form = new HistoryForm(null);
            form.Prompt = new PromptRecorder().Answer;
            form.ShowHistoryOf(TaskRelatedType.Client, ana.Id, "Ana García Pérez");
            DataGridView grid = FormAccess.Grid(form, "historyDataGridView");

            var navigations = new List<(Crm1.WindowsForms.Services.HistoryEntryKind Kind, Guid? TargetId)>();
            form.NavigateRequested += (kind, targetId) => navigations.Add((kind, targetId));

            System.Reflection.MethodInfo doubleClick = typeof(HistoryForm).GetMethod(
                "HistoryDataGridView_CellDoubleClick",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

            // Línea de recordatorio detallado: pide su registro concreto.
            doubleClick.Invoke(form, new object?[]
            {
                grid, new DataGridViewCellEventArgs(0, IndexOfRow(grid, "Recordatorio"))
            });

            // Línea de alta: solo el tipo (MainForm ya conoce al registro).
            doubleClick.Invoke(form, new object?[]
            {
                grid, new DataGridViewCellEventArgs(0, IndexOfRow(grid, "Alta del cliente"))
            });

            Assert.Equal(2, navigations.Count);
            Assert.Equal(Crm1.WindowsForms.Services.HistoryEntryKind.Reminder, navigations[0].Kind);
            Assert.Equal(active.Id, navigations[0].TargetId);
            Assert.Equal(Crm1.WindowsForms.Services.HistoryEntryKind.Registration, navigations[1].Kind);
            Assert.Null(navigations[1].TargetId);
        });
    }

    // Fila de la tabla con el texto de tipo indicado en la columna Tipo.
    private static int IndexOfRow(DataGridView grid, string typeText)
    {
        for (int i = 0; i < grid.Rows.Count; i++)
        {
            if (FormAccess.CellText(grid.Rows[i], "typeColumn") == typeText)
            {
                return i;
            }
        }

        throw new InvalidOperationException($"Fila no encontrada en el historial: {typeText}");
    }
}
