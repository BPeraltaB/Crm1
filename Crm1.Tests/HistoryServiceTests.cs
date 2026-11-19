using Crm1.WindowsForms.Models;
using Crm1.WindowsForms.Services;
using Xunit;
using Task = Crm1.WindowsForms.Models.Task;
using TaskStatus = Crm1.WindowsForms.Models.TaskStatus;

namespace Crm1.Tests;

/// <summary>
/// Pruebas de la agregación del historial (specs/010, sección 2): se
/// detallan solo las tareas y recordatorios que faltan, el resto se resume
/// por año (consumo, presupuestos, tareas completadas y recordatorios
/// descartados), con los totales globales de la barra.
/// </summary>
public sealed class HistoryServiceTests
{
    // Registros de prueba: cliente con fecha de alta y futuro sin ella.
    private static Client ClientOf(DateTime registrationDate) => new()
    {
        Id = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"),
        FirstName = "Ana",
        RegistrationDate = registrationDate
    };

    private static Contact ProspectOf(DateTime registrationDate) => new()
    {
        Id = Guid.Parse("fedcba98-7654-3210-fedc-ba9876543210"),
        FirstName = "Clara",
        RegistrationDate = registrationDate
    };

    private static Task TaskOf(Guid relatedId, TaskRelatedType relatedType,
        DateTime dueDate, string action = "Llamar", string description = "",
        TaskStatus status = TaskStatus.Pending) => new()
    {
        Id = Guid.NewGuid(),
        Action = action,
        Description = description,
        DueDate = dueDate,
        Status = status,
        RelatedId = relatedId,
        RelatedType = relatedType
    };

    private static Reminder ReminderOf(Guid relatedId, TaskRelatedType relatedType,
        DateTime remindAt, string subject = "Llamar para presupuesto",
        ReminderStatus status = ReminderStatus.Active) => new()
    {
        Id = Guid.NewGuid(),
        Subject = subject,
        RemindAt = remindAt,
        Status = status,
        RelatedId = relatedId,
        RelatedType = relatedType
    };

    private static Quote QuoteOf(Guid relatedId, TaskRelatedType relatedType,
        DateTime date, decimal amount = 1200.56m, string reference = "PRE-003",
        string description = "Reforma cocina", QuoteStatus status = QuoteStatus.Pending) => new()
    {
        Id = Guid.NewGuid(),
        QuoteDate = date,
        Amount = amount,
        Reference = reference,
        Description = description,
        Status = status,
        RelatedId = relatedId,
        RelatedType = relatedType
    };

    private static Order OrderOf(Guid clientId, DateTime date,
        OrderKind kind = OrderKind.Order, decimal amount = 1000m,
        string reference = "PRE-003", string description = "") => new()
    {
        Id = Guid.NewGuid(),
        Kind = kind,
        OrderDate = date,
        Amount = amount,
        Reference = reference,
        Description = description,
        ClientId = clientId
    };

    private static HistoryResult Build(Client? client, Contact? contact,
        params object[] activity) =>
        HistoryService.Build(
            client, contact,
            activity.OfType<Task>(),
            activity.OfType<Reminder>(),
            activity.OfType<Quote>(),
            activity.OfType<Order>());

    // Navegación con doble clic (specs/010 §6): las líneas detalladas llevan
    // el identificador de su registro; el alta y los resúmenes no llevan.
    [Fact]
    public void Detail_lines_expose_their_target_id_and_summaries_do_not()
    {
        Client ana = ClientOf(new DateTime(2025, 1, 15));
        Guid id = ana.Id;
        Task task = TaskOf(id, TaskRelatedType.Client, new DateTime(2026, 11, 10));
        Reminder reminder = ReminderOf(id, TaskRelatedType.Client, new DateTime(2026, 11, 20, 10, 30, 0));

        HistoryResult history = Build(ana, null,
            task,
            reminder,
            TaskOf(id, TaskRelatedType.Client, new DateTime(2026, 3, 5), status: TaskStatus.Completed),
            QuoteOf(id, TaskRelatedType.Client, new DateTime(2026, 11, 12), status: QuoteStatus.Accepted),
            OrderOf(id, new DateTime(2026, 11, 12), amount: 1000m));

        Assert.Equal(task.Id, history.Entries.Single(entry => entry.Kind == HistoryEntryKind.Task).TargetId);
        Assert.Equal(reminder.Id, history.Entries.Single(entry => entry.Kind == HistoryEntryKind.Reminder).TargetId);

        Assert.All(
            history.Entries.Where(entry => entry.Kind is not (HistoryEntryKind.Task or HistoryEntryKind.Reminder)),
            entry => Assert.Null(entry.TargetId));
    }

    [Fact]
    public void Client_history_details_only_what_is_outstanding_and_summarizes_by_year()    {
        Client ana = ClientOf(new DateTime(2025, 1, 15));
        Guid id = ana.Id;

        HistoryResult history = Build(ana, null,
            // Detalladas: pendientes y activos, de cualquier año.
            TaskOf(id, TaskRelatedType.Client, new DateTime(2026, 11, 10), "Enviar correo", "propuesta reformada"),
            ReminderOf(id, TaskRelatedType.Client, new DateTime(2026, 11, 20, 10, 30, 0)),
            // Resumidas: completadas y descartadas.
            TaskOf(id, TaskRelatedType.Client, new DateTime(2026, 3, 5), status: TaskStatus.Completed),
            ReminderOf(id, TaskRelatedType.Client, new DateTime(2025, 5, 2, 9, 0, 0), status: ReminderStatus.Dismissed),
            // Presupuestos y movimientos de dos años.
            QuoteOf(id, TaskRelatedType.Client, new DateTime(2026, 11, 12), status: QuoteStatus.Accepted),
            QuoteOf(id, TaskRelatedType.Client, new DateTime(2026, 11, 13), status: QuoteStatus.Accepted),
            QuoteOf(id, TaskRelatedType.Client, new DateTime(2026, 11, 14), status: QuoteStatus.Dismissed),
            QuoteOf(id, TaskRelatedType.Client, new DateTime(2026, 11, 15)),
            QuoteOf(id, TaskRelatedType.Client, new DateTime(2025, 2, 1), status: QuoteStatus.Accepted),
            OrderOf(id, new DateTime(2026, 11, 12), amount: 1000m),
            OrderOf(id, new DateTime(2026, 11, 20), amount: 500m),
            OrderOf(id, new DateTime(2026, 11, 22), OrderKind.CreditNote, 150m),
            OrderOf(id, new DateTime(2025, 6, 10), amount: 500m));

        // Bloque 2026: consumo, presupuestos y tareas completadas; luego el
        // detalle vivo; bloque 2025 igual; y el alta como línea más antigua.
        Assert.Equal(new[]
        {
            HistoryEntryKind.ConsumptionSummary,   // 2026
            HistoryEntryKind.QuoteSummary,        // 2026
            HistoryEntryKind.TaskSummary,         // 2026
            HistoryEntryKind.Reminder,            // 20/11/2026 10:30
            HistoryEntryKind.Task,                // 10/11/2026
            HistoryEntryKind.ConsumptionSummary,  // 2025
            HistoryEntryKind.QuoteSummary,        // 2025
            HistoryEntryKind.ReminderSummary,     // 2025
            HistoryEntryKind.Registration         // 15/01/2025
        }, history.Entries.Select(entry => entry.Kind).ToArray());

        // Las líneas de resumen encabezan su año con el 31/12 a última hora.
        Assert.Equal(new DateTime(2026, 12, 31, 23, 59, 59), history.Entries[0].Date);
        Assert.Equal(new DateTime(2025, 12, 31, 23, 59, 59), history.Entries[5].Date);

        Assert.Equal("Consumo 2026",
            history.Entries[0].TypeText);
        Assert.Equal("2 pedidos — 1.500,00 € · 1 abono — −150,00 € · Consumido: 1.350,00 €",
            history.Entries[0].Detail);
        Assert.Equal("Presupuestos 2026", history.Entries[1].TypeText);
        Assert.Equal("4 — 2 aceptados · 1 descartado · 1 pendiente", history.Entries[1].Detail);
        Assert.Equal("Tareas 2026", history.Entries[2].TypeText);
        Assert.Equal("1 completada", history.Entries[2].Detail);

        Assert.Equal("Recordatorio", history.Entries[3].TypeText);
        Assert.Equal("Activo", history.Entries[3].StatusText);
        Assert.Equal("Tarea", history.Entries[4].TypeText);
        Assert.Equal("Enviar correo: propuesta reformada", history.Entries[4].Detail);
        Assert.Equal("Pendiente", history.Entries[4].StatusText);

        Assert.Equal("Consumo 2025", history.Entries[5].TypeText);
        Assert.Equal("1 pedido — 500,00 € · Consumido: 500,00 €", history.Entries[5].Detail);
        Assert.Equal("Presupuestos 2025", history.Entries[6].TypeText);
        Assert.Equal("1 — 1 aceptado", history.Entries[6].Detail);
        Assert.Equal("Recordatorios 2025", history.Entries[7].TypeText);
        Assert.Equal("1 descartado", history.Entries[7].Detail);

        Assert.Equal("Alta del cliente", history.Entries[8].TypeText);
        Assert.Equal(string.Empty, history.Entries[8].Detail);
        Assert.Equal(string.Empty, history.Entries[8].StatusText);
    }

    [Fact]
    public void Summary_lines_carry_no_status_and_detail_rows_pluralize()
    {
        Client ana = ClientOf(new DateTime(2026, 1, 15));
        Guid id = ana.Id;

        HistoryResult history = Build(ana, null,
            TaskOf(id, TaskRelatedType.Client, new DateTime(2026, 3, 1), status: TaskStatus.Completed),
            TaskOf(id, TaskRelatedType.Client, new DateTime(2026, 3, 2), status: TaskStatus.Completed),
            ReminderOf(id, TaskRelatedType.Client, new DateTime(2026, 3, 3), status: ReminderStatus.Dismissed),
            ReminderOf(id, TaskRelatedType.Client, new DateTime(2026, 3, 4), status: ReminderStatus.Dismissed),
            TaskOf(id, TaskRelatedType.Client, new DateTime(2026, 11, 10), "Visitar"),
            OrderOf(id, new DateTime(2026, 5, 5), OrderKind.CreditNote, 150m));

        Assert.Equal("2 completadas",
            history.Entries.Single(entry => entry.Kind == HistoryEntryKind.TaskSummary).Detail);
        Assert.Equal("2 descartados",
            history.Entries.Single(entry => entry.Kind == HistoryEntryKind.ReminderSummary).Detail);

        // Con un año de solo abonos, el resumen no muestra «0 pedidos».
        Assert.Equal("1 abono — −150,00 € · Consumido: −150,00 €",
            history.Entries.Single(entry => entry.Kind == HistoryEntryKind.ConsumptionSummary).Detail);

        // Sin estado en las líneas de resumen; la tarea sin descripción
        // muestra solo su acción.
        Assert.All(history.Entries.Where(entry => entry.Kind >= HistoryEntryKind.ReminderSummary),
            entry => Assert.Equal(string.Empty, entry.StatusText));
        HistoryEntry pending = history.Entries.Single(entry => entry.Kind == HistoryEntryKind.Task);
        Assert.Equal("Visitar", pending.Detail);
        Assert.Equal("Pendiente", pending.StatusText);
    }

    [Fact]
    public void In_progress_tasks_are_detailed_and_empty_years_put_no_line()
    {
        Client ana = ClientOf(new DateTime(2025, 1, 15));
        Guid id = ana.Id;

        HistoryResult history = Build(ana, null,
            TaskOf(id, TaskRelatedType.Client, new DateTime(2026, 11, 10), status: TaskStatus.InProgress),
            TaskOf(id, TaskRelatedType.Client, new DateTime(2025, 3, 5), status: TaskStatus.Completed));

        // 2026 no tiene resumen de nada: solo la tarea en curso detallada.
        Assert.Equal(HistoryEntryKind.Task, history.Entries[0].Kind);
        Assert.Equal("En curso", history.Entries[0].StatusText);
        Assert.Equal(HistoryEntryKind.TaskSummary, history.Entries[1].Kind);
        Assert.Equal("Tareas 2025", history.Entries[1].TypeText);
        Assert.Equal(HistoryEntryKind.Registration, history.Entries[2].Kind);
    }

    [Fact]
    public void Client_without_registration_date_has_no_alta_line()
    {
        Client ana = ClientOf(default);
        Guid id = ana.Id;

        HistoryResult history = Build(ana, null,
            TaskOf(id, TaskRelatedType.Client, new DateTime(2026, 11, 10)));

        HistoryEntry entry = Assert.Single(history.Entries);
        Assert.Equal(HistoryEntryKind.Task, entry.Kind);
    }

    [Fact]
    public void Prospect_history_skips_orders_and_uses_its_own_alta()
    {
        Contact clara = ProspectOf(new DateTime(2026, 3, 1));
        Guid id = clara.Id;
        Client otro = ClientOf(new DateTime(2026, 1, 15));

        HistoryResult history = Build(null, clara,
            TaskOf(id, TaskRelatedType.Prospect, new DateTime(2026, 11, 10)),
            QuoteOf(id, TaskRelatedType.Prospect, new DateTime(2026, 11, 12), status: QuoteStatus.Accepted),
            // Pedidos de otro cliente: nunca en el historial de un futuro.
            OrderOf(otro.Id, new DateTime(2026, 11, 20)),
            OrderOf(otro.Id, new DateTime(2026, 11, 20), OrderKind.CreditNote));

        // Presupuestos 2026, tarea detallada y alta del futuro; sin consumo.
        Assert.Equal(new[]
        {
            HistoryEntryKind.QuoteSummary,
            HistoryEntryKind.Task,
            HistoryEntryKind.Registration
        }, history.Entries.Select(entry => entry.Kind).ToArray());
        Assert.Equal("Alta del futuro cliente", history.Entries[2].TypeText);

        // Totales sin movimientos: un futuro no puede tener pedidos.
        Assert.Equal(0, history.Totals.OrderCount);
        Assert.Equal(0, history.Totals.CreditNoteCount);
        Assert.Equal(0m, history.Totals.Consumed);
    }

    [Fact]
    public void Other_records_activity_is_excluded()
    {
        Client ana = ClientOf(new DateTime(2026, 1, 15));
        Client luis = ClientOf(new DateTime(2026, 1, 15));
        luis.Id = Guid.NewGuid();
        Contact clara = ProspectOf(default);

        HistoryResult history = Build(ana, null,
            TaskOf(luis.Id, TaskRelatedType.Client, new DateTime(2026, 11, 10)),
            TaskOf(clara.Id, TaskRelatedType.Prospect, new DateTime(2026, 11, 11)),
            QuoteOf(luis.Id, TaskRelatedType.Client, new DateTime(2026, 11, 12)),
            OrderOf(luis.Id, new DateTime(2026, 11, 12)));

        // Solo queda la línea de alta de Ana.
        HistoryEntry entry = Assert.Single(history.Entries);
        Assert.Equal(HistoryEntryKind.Registration, entry.Kind);
        Assert.Equal(0, history.Totals.QuoteCount);
        Assert.Equal(0, history.Totals.OrderCount);
    }

    [Fact]
    public void Totals_compute_the_consumed_as_net()
    {
        Client ana = ClientOf(new DateTime(2026, 1, 15));
        Guid id = ana.Id;

        HistoryResult history = Build(ana, null,
            QuoteOf(id, TaskRelatedType.Client, new DateTime(2026, 11, 10), status: QuoteStatus.Accepted),
            QuoteOf(id, TaskRelatedType.Client, new DateTime(2026, 11, 11), status: QuoteStatus.Accepted),
            QuoteOf(id, TaskRelatedType.Client, new DateTime(2026, 11, 12), status: QuoteStatus.Dismissed),
            QuoteOf(id, TaskRelatedType.Client, new DateTime(2026, 11, 13)),
            QuoteOf(id, TaskRelatedType.Client, new DateTime(2026, 11, 14)),
            OrderOf(id, new DateTime(2026, 11, 20), amount: 1000m),
            OrderOf(id, new DateTime(2026, 11, 21), amount: 500m),
            OrderOf(id, new DateTime(2026, 11, 22), OrderKind.CreditNote, 150m));

        Assert.Equal(5, history.Totals.QuoteCount);
        Assert.Equal(2, history.Totals.AcceptedQuotes);
        Assert.Equal(1, history.Totals.DismissedQuotes);
        Assert.Equal(2, history.Totals.PendingQuotes);

        Assert.Equal(2, history.Totals.OrderCount);
        Assert.Equal(1500m, history.Totals.OrderTotal);
        Assert.Equal(1, history.Totals.CreditNoteCount);
        Assert.Equal(150m, history.Totals.CreditNoteTotal);
        Assert.Equal(1350m, history.Totals.Consumed);
    }

    [Fact]
    public void Totals_consumed_can_be_negative()
    {
        Client ana = ClientOf(new DateTime(2026, 1, 15));

        HistoryResult history = Build(ana, null,
            OrderOf(ana.Id, new DateTime(2026, 11, 20), amount: 100m),
            OrderOf(ana.Id, new DateTime(2026, 11, 21), OrderKind.CreditNote, 500m));

        Assert.Equal(-400m, history.Totals.Consumed);
    }

    [Fact]
    public void Record_without_activity_has_only_the_alta_line()
    {
        Client ana = ClientOf(new DateTime(2026, 1, 15));

        HistoryResult history = Build(ana, null);

        HistoryEntry entry = Assert.Single(history.Entries);
        Assert.Equal(HistoryEntryKind.Registration, entry.Kind);
        Assert.Equal(0, history.Totals.QuoteCount);
        Assert.Equal(0, history.Totals.OrderCount);
        Assert.Equal(0m, history.Totals.Consumed);
    }

    [Fact]
    public void Prospect_without_date_has_no_entries_at_all()
    {
        Contact clara = ProspectOf(default);

        HistoryResult history = Build(null, clara);

        Assert.Empty(history.Entries);
        Assert.Equal(0, history.Totals.QuoteCount);
    }
}
