namespace Crm1.WindowsForms.Models;

/// <summary>
/// Presupuesto pedido por un cliente o futuro cliente (specs/009): registro
/// ligero de la actividad comercial; el documento vive en el programa de
/// administración, aquí solo se conserva que se pidió, su precio y si se
/// aceptó. Al aceptarse nace un pedido enlazado por OrderId.
/// </summary>
public class Quote
{
    /// <summary>
    /// Identificador único del presupuesto.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Fecha del presupuesto (dd/MM/aaaa, sin hora).
    /// </summary>
    public DateTime QuoteDate { get; set; }

    /// <summary>
    /// Precio del presupuesto (siempre mayor que cero).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Nº de presupuesto del programa de administración (opcional).
    /// </summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>
    /// Descripción (opcional).
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Estado del presupuesto.
    /// </summary>
    public QuoteStatus Status { get; set; } = QuoteStatus.Pending;

    /// <summary>
    /// Registro vinculado: cliente o futuro cliente (obligatorio).
    /// </summary>
    public Guid RelatedId { get; set; }

    /// <summary>
    /// Tipo del registro vinculado (se reutiliza el de las tareas).
    /// </summary>
    public TaskRelatedType RelatedType { get; set; } = TaskRelatedType.Client;

    /// <summary>
    /// Pedido generado al aceptar el presupuesto; null si aún no tiene.
    /// </summary>
    public Guid? OrderId { get; set; }
}
