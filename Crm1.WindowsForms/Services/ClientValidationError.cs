namespace Crm1.WindowsForms.Services;

/// <summary>
/// Error de validación de un cliente sobre un campo concreto.
/// </summary>
public sealed class ClientValidationError
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
/// Error de validación asociado a una fila de la tabla de clientes.
/// </summary>
public sealed class RowValidationError
{
    /// <summary>
    /// Índice (base cero) del cliente dentro de la lista validada.
    /// </summary>
    public int RowIndex { get; init; }

    /// <summary>
    /// Error del campo concreto.
    /// </summary>
    public ClientValidationError Error { get; init; } = new();
}
