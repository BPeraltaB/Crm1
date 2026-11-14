using Crm1.WindowsForms.Models;
using Crm1.WindowsForms.Services;
using Xunit;

namespace Crm1.Tests;

/// <summary>
/// Pruebas de las reglas de negocio de los recordatorios (specs/007,
/// sección 5): una por regla más un recordatorio válido completo.
/// </summary>
public sealed class ReminderServiceTests
{
    // Registros vinculables de prueba: un cliente y un futuro cliente.
    private static ReminderService Service(params (Guid Id, TaskRelatedType Type)[] related)
    {
        var records = related.ToDictionary(item => item.Id, item => item.Type);
        return new ReminderService(records);
    }

    private static Guid ClientId { get; } = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");

    private static Guid ProspectId { get; } = Guid.Parse("fedcba98-7654-3210-fedc-ba9876543210");

    private static Reminder ValidReminder() => new()
    {
        Id = Guid.NewGuid(),
        Subject = "Llamada de seguimiento del presupuesto",
        RemindAt = new DateTime(2026, 12, 1, 10, 30, 0),
        Status = ReminderStatus.Active,
        RelatedId = ClientId,
        RelatedType = TaskRelatedType.Client
    };

    [Fact]
    public void Valid_reminder_passes()
    {
        ReminderValidationError? error = Service((ClientId, TaskRelatedType.Client)).Validate(ValidReminder());

        Assert.Null(error);
    }

    [Fact]
    public void Empty_subject_is_rejected()
    {
        Reminder reminder = ValidReminder();
        reminder.Subject = " ";

        ReminderValidationError? error = Service((ClientId, TaskRelatedType.Client)).Validate(reminder);

        Assert.NotNull(error);
        Assert.Equal(nameof(Reminder.Subject), error!.Field);
        Assert.Equal("La celda está vacía.", error.Message);
    }

    [Fact]
    public void Missing_related_record_is_rejected()
    {
        Reminder reminder = ValidReminder();
        reminder.RelatedId = Guid.NewGuid();

        ReminderValidationError? error = Service((ClientId, TaskRelatedType.Client)).Validate(reminder);

        Assert.NotNull(error);
        Assert.Equal(nameof(Reminder.RelatedId), error!.Field);
        Assert.Equal("Debes elegir un cliente o futuro cliente.", error.Message);
    }

    [Fact]
    public void Related_type_mismatch_is_rejected()
    {
        // El Id existe pero es de un futuro cliente y el recordatorio dice cliente.
        Reminder reminder = ValidReminder();
        reminder.RelatedId = ProspectId;
        reminder.RelatedType = TaskRelatedType.Client;

        ReminderValidationError? error =
            Service((ClientId, TaskRelatedType.Client), (ProspectId, TaskRelatedType.Prospect)).Validate(reminder);

        Assert.NotNull(error);
        Assert.Equal(nameof(Reminder.RelatedId), error!.Field);
    }

    [Fact]
    public void Missing_date_is_rejected()
    {
        Reminder reminder = ValidReminder();
        reminder.RemindAt = default;

        ReminderValidationError? error = Service((ClientId, TaskRelatedType.Client)).Validate(reminder);

        Assert.NotNull(error);
        Assert.Equal(nameof(Reminder.RemindAt), error!.Field);
        Assert.Equal("La celda está vacía.", error.Message);
    }

    [Fact]
    public void Invalid_status_is_rejected()
    {
        Reminder reminder = ValidReminder();
        reminder.Status = (ReminderStatus)99;

        ReminderValidationError? error = Service((ClientId, TaskRelatedType.Client)).Validate(reminder);

        Assert.NotNull(error);
        Assert.Equal(nameof(Reminder.Status), error!.Field);
        Assert.Equal("El estado no es válido.", error.Message);
    }

    [Theory]
    [InlineData(ReminderStatus.Active)]
    [InlineData(ReminderStatus.Dismissed)]
    public void Both_statuses_are_allowed(ReminderStatus status)
    {
        Reminder reminder = ValidReminder();
        reminder.Status = status;

        Assert.Null(Service((ClientId, TaskRelatedType.Client)).Validate(reminder));
    }

    [Fact]
    public void ValidateReminders_reports_row_indexes()
    {
        Reminder valid = ValidReminder();

        Reminder emptySubject = ValidReminder();
        emptySubject.Subject = string.Empty;

        Reminder noDate = ValidReminder();
        noDate.RemindAt = default;

        List<ReminderRowValidationError> errors = Service((ClientId, TaskRelatedType.Client))
            .ValidateReminders(new List<Reminder> { valid, emptySubject, noDate });

        Assert.Equal(2, errors.Count);
        Assert.Equal(1, errors[0].RowIndex);
        Assert.Equal(nameof(Reminder.Subject), errors[0].Error.Field);
        Assert.Equal(2, errors[1].RowIndex);
        Assert.Equal(nameof(Reminder.RemindAt), errors[1].Error.Field);
    }

    // Momento de referencia de las reglas de aviso: 18/11/2026 a las 08:00.
    private static readonly DateTime Now = new(2026, 11, 18, 8, 0, 0);

    private static Reminder At(DateTime remindAt, ReminderStatus status = ReminderStatus.Active) => new()
    {
        Id = Guid.NewGuid(),
        Subject = "Asunto de prueba",
        RemindAt = remindAt,
        Status = status,
        RelatedId = ClientId,
        RelatedType = TaskRelatedType.Client
    };

    [Fact]
    public void DueOnOpen_includes_overdue_and_today_regardless_of_hour()
    {
        // Vencido de ayer y uno de HOY a las 23:00 (aún por llegar): ambos
        // avisan al abrir (comparación por fecha, specs/007 §5).
        Reminder overdue = At(Now.AddDays(-1).AddHours(-2));
        Reminder todayLate = At(Now.Date.AddHours(23));
        Reminder dismissed = At(Now.AddDays(-2), ReminderStatus.Dismissed);

        List<Reminder> due = ReminderService.GetDueOnOpen(new[] { todayLate, dismissed, overdue }, Now);

        Assert.Equal(2, due.Count);
        Assert.Equal(overdue.Id, due[0].Id);   // orden ascendente por fecha
        Assert.Equal(todayLate.Id, due[1].Id);
    }

    [Fact]
    public void DueOnOpen_excludes_tomorrow_and_later()
    {
        List<Reminder> due = ReminderService.GetDueOnOpen(new[]
        {
            At(Now.AddDays(1)),
            At(Now.AddDays(2)),
            At(Now.AddDays(10))
        }, Now);

        Assert.Empty(due);
    }

    [Fact]
    public void DueOnClose_includes_overdue_today_and_tomorrow()
    {
        Reminder overdue = At(Now.AddDays(-3));
        Reminder today = At(Now.Date.AddHours(12));
        Reminder tomorrow = At(Now.AddDays(1).Date.AddHours(9));

        List<Reminder> due = ReminderService.GetDueOnClose(new[] { tomorrow, today, overdue }, Now);

        Assert.Equal(3, due.Count);
        Assert.Equal(overdue.Id, due[0].Id);
        Assert.Equal(today.Id, due[1].Id);
        Assert.Equal(tomorrow.Id, due[2].Id);
    }

    [Fact]
    public void DueOnClose_excludes_the_day_after_tomorrow()
    {
        List<Reminder> due = ReminderService.GetDueOnClose(new[]
        {
            At(Now.AddDays(2)),
            At(Now.AddDays(5))
        }, Now);

        Assert.Empty(due);
    }

    [Fact]
    public void Dismissed_reminders_never_warn()
    {
        var dismissed = new[]
        {
            At(Now.AddDays(-1), ReminderStatus.Dismissed),
            At(Now, ReminderStatus.Dismissed),
            At(Now.AddDays(1), ReminderStatus.Dismissed)
        };

        Assert.Empty(ReminderService.GetDueOnOpen(dismissed, Now));
        Assert.Empty(ReminderService.GetDueOnClose(dismissed, Now));
    }
}
