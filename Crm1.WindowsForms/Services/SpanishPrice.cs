using System.Globalization;

namespace Crm1.WindowsForms.Services;

/// <summary>
/// Lectura y escritura de precios en formato español (specs/009): se admiten
/// «1.234,56», «1234,56» y «1234.56» y se muestran con separador de miles y
/// dos decimales («1.234,56»). Vive fuera de los formularios (constitución,
/// punto 3) y lo usan los servicios de presupuestos y pedidos.
/// </summary>
public static class SpanishPrice
{
    // Cultura española fija: los separadores no dependen de la configuración
    // de Windows del usuario.
    private static readonly CultureInfo SpanishCulture = new("es-ES");

    /// <summary>
    /// Intenta leer un precio escrito en formato español. Reglas: si hay coma,
    /// es el separador decimal y los puntos son miles; si solo hay puntos, un
    /// punto con exactamente tres dígitos detrás son miles («1.234») y el
    /// resto, decimales («1.50»); espacios y el símbolo € se ignoran.
    /// </summary>
    public static bool TryParse(string text, out decimal amount)
    {
        amount = 0m;
        string cleaned = (text ?? string.Empty).Trim().Replace("€", string.Empty).Replace(" ", string.Empty);
        if (cleaned.Length == 0)
        {
            return false;
        }

        string normalized = cleaned;
        if (cleaned.Contains(','))
        {
            // Con coma, esta es el separador decimal y los puntos son miles.
            normalized = cleaned.Replace(".", string.Empty).Replace(',', '.');
        }
        else if (cleaned.Contains('.'))
        {
            string[] parts = cleaned.Split('.');
            if (parts.Length == 2 && parts[1].Length != 3)
            {
                // Un solo punto con 1-2 decimales: separador decimal.
                normalized = cleaned;
            }
            else
            {
                // Separadores de miles: el primer grupo de 1 a 3 dígitos y
                // los demás de 3 exactos («1.234», «1.234.567»); el resto,
                // texto mal formado que se rechaza («1.23.45»).
                bool validGroups = parts[0].Length is >= 1 and <= 3
                    && parts.Skip(1).All(part => part.Length == 3);
                if (!validGroups)
                {
                    return false;
                }
                normalized = string.Concat(parts);
            }
        }

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }

    /// <summary>
    /// Formatea un precio en formato español con dos decimales («1.234,56»).
    /// </summary>
    public static string Format(decimal amount) => amount.ToString("N2", SpanishCulture);
}
