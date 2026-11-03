using Crm1.WindowsForms.Models;

namespace Crm1.WindowsForms.Services;

/// <summary>
/// Resultado de convertir un futuro cliente en cliente (specs/003, sección 7):
/// el cliente listo para dar de alta si todo es correcto, o la lista de
/// carencias que lo impiden, una por campo.
/// </summary>
public sealed class ConversionResult
{
    /// <summary>
    /// Cliente construido a partir del futuro cliente, con identificador nuevo.
    /// Nulo si la validación falló.
    /// </summary>
    public Client? Client { get; init; }

    /// <summary>
    /// Carencias que impiden la conversión, una por campo, con el nombre del
    /// campo y el motivo ("Teléfono: el cliente exige un móvil…").
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Verdadero cuando el futuro cliente puede convertirse en cliente.
    /// </summary>
    public bool Success => Client is not null && Errors.Count == 0;
}
