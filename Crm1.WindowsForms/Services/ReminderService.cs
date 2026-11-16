using Crm1.WindowsForms.Models;

namespace Crm1.WindowsForms.Services;

/// <summary>
/// Error de validación de un recordatorio sobre un campo concreto.
/// </summary>
public sealed class ReminderValidationError
{
    /// <summary>
    /// Campo del modelo afectado (nombre de la propiedad, en inglés).
    /// </summary>
    public string Field { get; init; } = string.Empty;

    /// <summary>
    /// Mensaje de error para el usuario, en español.
    /// </summary>
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Error de validación asociado a una fila de la tabla de recordatorios.
/// </summary>
public sealed class ReminderRowValidationError
{
    /// <summary>
    /// Índice (base cero) del recordatorio dentro de la lista validada.
    /// </summary>
    public int RowIndex { get; init; }

    /// <summary>
    /// Error del campo concreto.
    /// </summary>
    public ReminderValidationError Error { get; init; } = new();
}

/// <summary>
/// Reglas de negocio de los recordatorios (specs/007): el asunto es
/// obligatorio, el vínculo con un cliente o futuro cliente existente es
/// obligatorio, la fecha y hora deben estar presentes y el estado pertenece
/// a su desplegable. Vive fuera de los formularios (constitución, punto 3).
/// Las reglas de aviso al abrir y cerrar la aplicación llegan con la
/// integración (specs/007, sección 7).
/// </summary>
public sealed class ReminderService
{
    // Registros vinculables conocidos: Id → tipo (lo construye el formulario
    // con los clientes y futuros clientes de la base).
    private readonly IReadOnlyDictionary<Guid, TaskRelatedType> _relatedRecords;

    /// <summary>
    /// Crea el servicio con los registros vinculables existentes en la base.
    /// </summary>
    public ReminderService(IReadOnlyDictionary<Guid, TaskRelatedType> relatedRecords)
    {
        _relatedRecords = relatedRecords;
    }

    /// <summary>
    /// Valida un recordatorio concreto; devuelve el error del primer campo
    /// que falla o null si es correcto.
    /// </summary>
    public ReminderValidationError? Validate(Reminder reminder)
    {
        if (string.IsNullOrWhiteSpace(reminder.Subject))
        {
            return new ReminderValidationError
            {
                Field = nameof(Reminder.Subject),
                Message = "La celda está vacía."
            };
        }

        if (!_relatedRecords.TryGetValue(reminder.RelatedId, out TaskRelatedType type)
            || type != reminder.RelatedType)
        {
            return new ReminderValidationError
            {
                Field = nameof(Reminder.RelatedId),
                Message = "Debes elegir un cliente o futuro cliente."
            };
        }

        if (reminder.RemindAt == default)
        {
            return new ReminderValidationError
            {
                Field = nameof(Reminder.RemindAt),
                Message = "La celda está vacía."
            };
        }

        if (reminder.Status is not (ReminderStatus.Active or ReminderStatus.Dismissed))
        {
            return new ReminderValidationError
            {
                Field = nameof(Reminder.Status),
                Message = "El estado no es válido."
            };
        }

        return null;
    }

    /// <summary>
    /// Valida todos los recordatorios y devuelve un error por fila defectuosa.
    /// </summary>
    public List<ReminderRowValidationError> ValidateReminders(List<Reminder> reminders)
    {
        var errors = new List<ReminderRowValidationError>();
        for (int i = 0; i < reminders.Count; i++)
        {
            ReminderValidationError? error = Validate(reminders[i]);
            if (error is not null)
            {
                errors.Add(new ReminderRowValidationError { RowIndex = i, Error = error });
            }
        }
        return errors;
    }

    // Antigüedad máxima de los vencidos que avisan (decisión del usuario,
    // 14/11/2026): los vencidos hace más días dejan de salir en los diálogos
    // y quedan como historial en la tabla.
    private const int OverdueWindowDays = 5;

    /// <summary>
    /// Recordatorios que deben avisarse al abrir la aplicación (specs/007
    /// §5): los Activos vencidos con cinco días de antigüedad como mucho y
    /// los de hoy, ordenados por fecha y hora ascendente. Los vencidos más
    /// antiguos no avisan (quedan como historial en la tabla, sin
    /// descartarse solos). La comparación es por fecha: uno de hoy a las
    /// 23:00 avisa también a las 08:00. Los Descartados nunca avisan.
    /// </summary>
    public static List<Reminder> GetDueOnOpen(IEnumerable<Reminder> reminders, DateTime now) =>
        reminders
            .Where(reminder => reminder.Status == ReminderStatus.Active
                && reminder.RemindAt.Date >= now.Date.AddDays(-OverdueWindowDays)
                && reminder.RemindAt.Date <= now.Date)
            .OrderBy(reminder => reminder.RemindAt)
            .ToList();

    /// <summary>
    /// Recordatorios que deben avisarse al cerrar la aplicación (specs/007
    /// §5): los Activos vencidos con cinco días de antigüedad como mucho
    /// —incluidos los de hoy— y los del día siguiente, ordenados por fecha y
    /// hora ascendente. Los Descartados nunca avisan.
    /// </summary>
    public static List<Reminder> GetDueOnClose(IEnumerable<Reminder> reminders, DateTime now) =>
        reminders
            .Where(reminder => reminder.Status == ReminderStatus.Active
                && reminder.RemindAt.Date >= now.Date.AddDays(-OverdueWindowDays)
                && reminder.RemindAt.Date <= now.Date.AddDays(1))
            .OrderBy(reminder => reminder.RemindAt)
            .ToList();
}
