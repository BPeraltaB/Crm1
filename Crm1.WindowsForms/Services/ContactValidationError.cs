namespace Crm1.WindowsForms.Services;

/// <summary>
/// Error de validación de un futuro cliente sobre un campo concreto.
/// </summary>
public sealed class ContactValidationError
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
/// Error de validación asociado a una fila de la tabla de futuros clientes.
/// </summary>
public sealed class ContactRowValidationError
{
    /// <summary>
    /// Índice (base cero) del futuro cliente dentro de la lista validada.
    /// </summary>
    public int RowIndex { get; init; }

    /// <summary>
    /// Error del campo concreto.
    /// </summary>
    public ContactValidationError Error { get; init; } = new();
}
