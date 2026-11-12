namespace Crm1.WindowsForms.Models;

/// <summary>
/// Tarea del CRM vinculada a un cliente o a un futuro cliente (specs/005).
/// </summary>
public class Task
{
    /// <summary>
    /// Identificador único de la tarea (no se muestra en la tabla).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Acción de la tarea: Llamar, Visitar, Enviar correo, Reunión u Otro
    /// (desplegable de la tabla; specs/005).
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Descripción de la tarea (opcional).
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Fecha límite de la tarea (obligatoria, sin hora).
    /// </summary>
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Estado de la tarea.
    /// </summary>
    public TaskStatus Status { get; set; } = TaskStatus.Pending;

    /// <summary>
    /// Prioridad de la tarea.
    /// </summary>
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    /// <summary>
    /// Identificador del cliente o futuro cliente vinculado (obligatorio).
    /// </summary>
    public Guid RelatedId { get; set; }

    /// <summary>
    /// Tipo del registro vinculado: cliente o futuro cliente.
    /// </summary>
    public TaskRelatedType RelatedType { get; set; } = TaskRelatedType.Client;
}
