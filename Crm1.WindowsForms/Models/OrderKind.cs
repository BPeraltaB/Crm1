namespace Crm1.WindowsForms.Models;

/// <summary>
/// Tipo de movimiento de un cliente: Pedido o Abono (los abonos restan en
/// los totales; etiquetas en español en la interfaz).
/// </summary>
public enum OrderKind
{
    Order,
    CreditNote
}
