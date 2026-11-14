namespace Crm1.WindowsForms.Models;

/// <summary>
/// Estado del recordatorio: o todavía tiene que avisar (Activo) o ya avisó
/// (Descartado). Un recordatorio no se «completa» (specs/007).
/// </summary>
public enum ReminderStatus
{
    /// <summary>Todavía tiene que avisar.</summary>
    Active,

    /// <summary>Ya avisó y se descartó; no vuelve a saltar.</summary>
    Dismissed
}

/// <summary>
/// Recordatorio del CRM: aviso puntual con fecha y hora, vinculado a un
/// cliente o a un futuro cliente (specs/007). A diferencia de la tarea
/// (specs/005), salta solo a su momento y se descarta: no tiene prioridad,
/// descripción ni «en curso».
/// </summary>
public class Reminder
{
    /// <summary>
    /// Identificador único del recordatorio (no se muestra en la tabla).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Asunto: lo que dice el aviso (obligatorio).
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora a la que salta el aviso (obligatoria).
    /// </summary>
    public DateTime RemindAt { get; set; }

    /// <summary>
    /// Estado del recordatorio.
    /// </summary>
    public ReminderStatus Status { get; set; } = ReminderStatus.Active;

    /// <summary>
    /// Identificador del cliente o futuro cliente vinculado (obligatorio).
    /// </summary>
    public Guid RelatedId { get; set; }

    /// <summary>
    /// Tipo del registro vinculado: cliente o futuro cliente (mismo tipo que
    /// las tareas, specs/005).
    /// </summary>
    public TaskRelatedType RelatedType { get; set; } = TaskRelatedType.Client;
}
