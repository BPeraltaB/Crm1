namespace Crm1.WindowsForms.Services;

/// <summary>
/// Corrección de mayúsculas según las normas de la lengua: nombre, apellidos,
/// dirección y ciudad con inicial mayúscula en cada palabra (las partículas
/// españolas quedan en minúscula si no abren el texto) y observaciones con
/// solo la primera letra del texto. Se aplica al guardar, sin impedir escribir
/// como se quiera (specs/002 y specs/003).
/// </summary>
public static class TextNormalizer
{
    // Partículas españolas que van en minúscula dentro del texto ("María de la Torre").
    private static readonly HashSet<string> LowercaseParticles = new(StringComparer.OrdinalIgnoreCase)
    {
        "de", "del", "la", "las", "los", "el", "y", "e"
    };

    // Siglas de sociedad que se conservan en mayúsculas ("Talleres Muñoz SA").
    private static readonly HashSet<string> CompanySuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "SA", "SL", "SLL", "SAU", "SAB", "SC"
    };

    /// <summary>
    /// Nombre propio, apellido, dirección o ciudad: inicial mayúscula en cada
    /// palabra y el resto en minúscula, salvo las partículas españolas (en
    /// minúscula si no abren el texto) y las siglas cortas como SA o SL, que se
    /// conservan tal cual. "las palmas de gran canaria" → "Las Palmas de Gran Canaria".
    /// </summary>
    public static string CapitalizeWords(string text)
    {
        string trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        var words = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var builder = new System.Text.StringBuilder(trimmed.Length);
        for (int i = 0; i < words.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(' ');
            }
            builder.Append(CapitalizeWord(words[i], isFirst: i == 0));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Observaciones: solo la primera letra del texto en mayúscula; el resto
    /// queda tal como lo escribió el usuario.
    /// </summary>
    public static string CapitalizeSentence(string text)
    {
        string trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        return char.ToUpperInvariant(trimmed[0]) + trimmed[1..];
    }

    // Corrige una sola palabra: partícula no inicial en minúscula, sigla de
    // sociedad en mayúsculas y el resto con inicial mayúscula y demás letras
    // en minúscula.
    private static string CapitalizeWord(string word, bool isFirst)
    {
        // Las partículas no iniciales van en minúscula ("María de la Torre").
        if (!isFirst && LowercaseParticles.Contains(word))
        {
            return word.ToLowerInvariant();
        }

        // Las siglas de sociedad se conservan en mayúsculas ("Muñoz SA").
        if (CompanySuffixes.Contains(word))
        {
            return word.ToUpperInvariant();
        }

        return char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant();
    }
}
