namespace Crm1.WindowsForms.Models;

/// <summary>
/// Pedido o abono de un cliente (specs/009): registro de la actividad
/// comercial. El pedido nace directamente o de un presupuesto aceptado; el
/// abono resta en los totales anuales. Solo clientes: un futuro cliente con
/// pedido ya sería cliente.
/// </summary>
public class Order
{
    /// <summary>
    /// Identificador único del movimiento.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tipo de movimiento: Pedido o Abono.
    /// </summary>
    public OrderKind Kind { get; set; } = OrderKind.Order;

    /// <summary>
    /// Fecha del pedido o abono (dd/MM/aaaa, sin hora).
    /// </summary>
    public DateTime OrderDate { get; set; }

    /// <summary>
    /// Precio (siempre mayor que cero; los abonos restan en los totales).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Nº de pedido o abono del programa de administración (opcional).
    /// </summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>
    /// Descripción (opcional).
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Cliente del movimiento (obligatorio; nunca un futuro cliente).
    /// </summary>
    public Guid ClientId { get; set; }
}
