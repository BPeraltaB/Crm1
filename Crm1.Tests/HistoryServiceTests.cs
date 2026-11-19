using Crm1.WindowsForms.Models;
using Crm1.WindowsForms.Services;
using Xunit;
using Task = Crm1.WindowsForms.Models.Task;
using TaskStatus = Crm1.WindowsForms.Models.TaskStatus;

namespace Crm1.Tests;

/// <summary>
/// Pruebas de la agregación del historial (specs/010, sección 2): mezcla de
/// tipos, orden cronológico inverso, alta condicionada a la fecha, textos de
/// la interfaz y totales con el consumo neto.
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

    [Fact]
    public void Client_history_mixes_all_kinds_newest_first()
    {
        Client ana = ClientOf(new DateTime(2026, 1, 15));
        Guid id = ana.Id;

        HistoryResult history = Build(ana, null,
            TaskOf(id, TaskRelatedType.Client, new DateTime(2026, 11, 10)),
            QuoteOf(id, TaskRelatedType.Client, new DateTime(2026, 11, 12)),
            OrderOf(id, new DateTime(2026, 11, 12)),
            ReminderOf(id, TaskRelatedType.Client, new DateTime(2026, 11, 20, 10, 30, 0)));

        // Cinco líneas: alta + recordatorio + pedido + presupuesto + tarea.
        Assert.Equal(5, history.Entries.Count);
        Assert.Equal(HistoryEntryKind.Reminder, history.Entries[0].Kind);
        Assert.Equal(HistoryEntryKind.Order, history.Entries[1].Kind);
        Assert.Equal(HistoryEntryKind.Quote, history.Entries[2].Kind);
        Assert.Equal(HistoryEntryKind.Task, history.Entries[3].Kind);
        Assert.Equal(HistoryEntryKind.Registration, history.Entries[4].Kind);

        // La línea de alta no tiene detalle ni estado.
        HistoryEntry alta = history.Entries[4];
        Assert.Equal("Alta del cliente", alta.TypeText);
        Assert.Equal(new DateTime(2026, 1, 15), alta.Date);
        Assert.Equal(string.Empty, alta.Detail);
        Assert.Equal(string.Empty, alta.StatusText);
    }

    [Fact]
    public void Same_day_puts_the_money_on_top()
    {
        Client ana = ClientOf(default);
        Guid id = ana.Id;

        HistoryResult history = Build(ana, null,
            TaskOf(id, TaskRelatedType.Client, new DateTime(2026, 11, 12)),
            QuoteOf(id, TaskRelatedType.Client, new DateTime(2026, 11, 12)),
            OrderOf(id, new DateTime(2026, 11, 12)),
            OrderOf(id, new DateTime(2026, 11, 12), OrderKind.CreditNote));

        Assert.Equal(
            new[] { HistoryEntryKind.CreditNote, HistoryEntryKind.Order, HistoryEntryKind.Quote, HistoryEntryKind.Task },
            history.Entries.Select(entry => entry.Kind).ToArray());
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
            QuoteOf(id, TaskRelatedType.Prospect, new DateTime(2026, 11, 12)),
            // Pedidos de otro cliente: nunca en el historial de un futuro.
            OrderOf(otro.Id, new DateTime(2026, 11, 20)),
            OrderOf(otro.Id, new DateTime(2026, 11, 20), OrderKind.CreditNote));

        Assert.Equal(3, history.Entries.Count);
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
        Contact clara = ProspectOf(default);
        luis.Id = Guid.NewGuid();

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
    public void Detail_and_status_texts_are_in_spanish()
    {
        Client ana = ClientOf(new DateTime(2026, 1, 15));
        Guid id = ana.Id;

        HistoryResult history = Build(ana, null,
            QuoteOf(id, TaskRelatedType.Client, new DateTime(2026, 11, 12),
                1000m, "PRE-003", "Reforma cocina", QuoteStatus.Accepted),
            TaskOf(id, TaskRelatedType.Client, new DateTime(2026, 11, 10),
                "Enviar correo", "propuesta reformada", TaskStatus.Completed),
            ReminderOf(id, TaskRelatedType.Client, new DateTime(2026, 11, 5),
                "Llamar para presupuesto", ReminderStatus.Dismissed),
            OrderOf(id, new DateTime(2026, 11, 20), OrderKind.CreditNote,
                150m, "DEV-001", "Abono parcial"));

        var byKind = history.Entries.ToDictionary(entry => entry.Kind);

        Assert.Equal("1.000,00 € · PRE-003 · Reforma cocina", byKind[HistoryEntryKind.Quote].Detail);
        Assert.Equal("Aceptado", byKind[HistoryEntryKind.Quote].StatusText);

        Assert.Equal("Enviar correo: propuesta reformada", byKind[HistoryEntryKind.Task].Detail);
        Assert.Equal("Completada", byKind[HistoryEntryKind.Task].StatusText);

        Assert.Equal("Llamar para presupuesto", byKind[HistoryEntryKind.Reminder].Detail);
        Assert.Equal("Descartado", byKind[HistoryEntryKind.Reminder].StatusText);

        // El abono lleva el menos tipográfico «−» delante del precio.
        Assert.Equal("−150,00 € · DEV-001 · Abono parcial", byKind[HistoryEntryKind.CreditNote].Detail);
        Assert.Equal(string.Empty, byKind[HistoryEntryKind.CreditNote].StatusText);
    }

    [Fact]
    public void Task_without_description_shows_only_the_action()
    {
        Client ana = ClientOf(new DateTime(2026, 1, 15));

        HistoryResult history = Build(ana, null,
            TaskOf(ana.Id, TaskRelatedType.Client, new DateTime(2026, 11, 10), "Visitar"));

        Assert.Equal("Tarea", history.Entries[0].TypeText);
        Assert.Equal("Visitar", history.Entries[0].Detail);
    }

    [Fact]
    public void Quote_detail_skips_empty_reference_and_description()
    {
        Client ana = ClientOf(new DateTime(2026, 1, 15));

        HistoryResult history = Build(ana, null,
            QuoteOf(ana.Id, TaskRelatedType.Client, new DateTime(2026, 11, 10),
                1000m, "", "", QuoteStatus.Dismissed));

        Assert.Equal("1.000,00 €", history.Entries[0].Detail);
        Assert.Equal("Descartado", history.Entries[0].StatusText);
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
