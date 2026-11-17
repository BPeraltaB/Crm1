using Crm1.WindowsForms.Models;

namespace Crm1.WindowsForms.Services;

/// <summary>
/// Error de validación de un pedido o abono sobre un campo concreto.
/// </summary>
public sealed class OrderValidationError
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
/// Error de validación asociado a una fila de la tabla de pedidos.
/// </summary>
public sealed class OrderRowValidationError
{
    /// <summary>
    /// Índice (base cero) del pedido dentro de la lista validada.
    /// </summary>
    public int RowIndex { get; init; }

    /// <summary>
    /// Error del campo concreto.
    /// </summary>
    public OrderValidationError Error { get; init; } = new();
}

/// <summary>
/// Reglas de negocio de los pedidos y abonos (specs/009): fecha obligatoria,
/// precio mayor que cero, tipo del desplegable y cliente existente — nunca un
/// futuro cliente, que con pedido ya sería cliente. Vive fuera de los
/// formularios (constitución, punto 3).
/// </summary>
public sealed class OrderService
{
    // Clientes conocidos: Id → existe (lo construye el formulario con los
    // clientes de la base; solo clientes, nunca futuros).
    private readonly IReadOnlyDictionary<Guid, TaskRelatedType> _relatedRecords;

    /// <summary>
    /// Crea el servicio con los clientes existentes en la base.
    /// </summary>
    public OrderService(IReadOnlyDictionary<Guid, TaskRelatedType> relatedRecords)
    {
        _relatedRecords = relatedRecords;
    }

    /// <summary>
    /// Valida un pedido o abono concreto; devuelve el error del primer campo
    /// que falla o null si es correcto. La fecha no puede ser anterior a hoy
    /// SOLO al dar de alta (isNewCreation): modificar un registro existente
    /// nunca se bloquea por su fecha histórica (decisión del usuario, 18/11).
    /// </summary>
    public OrderValidationError? Validate(Order order, bool isNewCreation = false)
    {
        if (order.OrderDate == default)
        {
            return new OrderValidationError
            {
                Field = nameof(Order.OrderDate),
                Message = "La celda está vacía."
            };
        }

        if (isNewCreation && order.OrderDate.Date < DateTime.Today)
        {
            return new OrderValidationError
            {
                Field = nameof(Order.OrderDate),
                Message = "La fecha no puede ser anterior a hoy."
            };
        }

        if (order.Amount <= 0)
        {
            return new OrderValidationError
            {
                Field = nameof(Order.Amount),
                Message = order.Amount == 0
                    ? "La celda está vacía."
                    : "El precio debe ser mayor que cero."
            };
        }

        // El movimiento es siempre de un cliente: un futuro cliente con
        // pedido ya sería cliente (specs/009 §1).
        if (!_relatedRecords.TryGetValue(order.ClientId, out TaskRelatedType type)
            || type != TaskRelatedType.Client)
        {
            return new OrderValidationError
            {
                Field = nameof(Order.ClientId),
                Message = "Debes elegir un cliente."
            };
        }

        return null;
    }

    /// <summary>
    /// Valida todos los pedidos y devuelve un error por fila defectuosa.
    /// </summary>
    public List<OrderRowValidationError> ValidateOrders(List<Order> orders)
    {
        var errors = new List<OrderRowValidationError>();
        for (int i = 0; i < orders.Count; i++)
        {
            OrderValidationError? error = Validate(orders[i]);
            if (error is not null)
            {
                errors.Add(new OrderRowValidationError { RowIndex = i, Error = error });
            }
        }
        return errors;
    }

    /// <summary>
    /// Totales de un conjunto de movimientos (specs/009 §4.2): cuántos pedidos
    /// y su suma, cuántos abonos y su resta, y el neto.
    /// </summary>
    public static (int OrderCount, decimal OrderTotal, int CreditNoteCount, decimal CreditNoteTotal, decimal Net) Totals(IEnumerable<Order> orders)
    {
        int orderCount = 0;
        decimal orderTotal = 0m;
        int creditNoteCount = 0;
        decimal creditNoteTotal = 0m;

        foreach (Order order in orders)
        {
            if (order.Kind == OrderKind.CreditNote)
            {
                creditNoteCount++;
                creditNoteTotal += order.Amount;
            }
            else
            {
                orderCount++;
                orderTotal += order.Amount;
            }
        }

        return (orderCount, orderTotal, creditNoteCount, creditNoteTotal, orderTotal - creditNoteTotal);
    }
}
