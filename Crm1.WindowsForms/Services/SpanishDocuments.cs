using System.Globalization;
using System.Text.RegularExpressions;
using Crm1.WindowsForms.Models;

namespace Crm1.WindowsForms.Services;

/// <summary>
/// Utilidades para detectar y validar los documentos de identificación fiscales
/// españoles (DNI, NIE y CIF), incluido el carácter de control.
/// </summary>
public static partial class SpanishDocuments
{
    // Letras de control válidas para DNI y NIE (índice = número % 23).
    private const string ControlLetters = "TRWAGMYFPDXBNJZSQVHLCKE";

    // Organizaciones cuyo CIF exige letra de control.
    private const string CifLetterControlOrganizations = "KPQSNW";

    // Organizaciones cuyo CIF exige dígito de control.
    private const string CifDigitControlOrganizations = "ABEH";

    // Letras válidas como control alfabético del CIF.
    private const string CifControlLetters = "JABCDEFGHI";

    [GeneratedRegex(@"^(\d{7,8})([A-Z])$")]
    private static partial Regex DniRegex();

    [GeneratedRegex(@"^([XYZ])(\d{7})([A-Z])$")]
    private static partial Regex NieRegex();

    [GeneratedRegex(@"^([A-HJ-NP-SUVW])(\d{7})([A-Z0-9])$")]
    private static partial Regex CifRegex();

    /// <summary>
    /// Normaliza un documento: sin espacios, en mayúsculas y con el DNI de 7 dígitos
    /// rellenado con un cero a la izquierda.
    /// </summary>
    public static string Normalize(string document)
    {
        string value = document.Trim().ToUpperInvariant();

        // Un DNI de 7 dígitos se completa con el cero inicial (formato actual de 8).
        if (DniRegex().IsMatch(value) && value.Length == 8)
        {
            value = value.PadLeft(9, '0');
        }

        return value;
    }

    /// <summary>
    /// Detecta el tipo de documento (DNI, NIE o CIF) a partir de su forma.
    /// </summary>
    public static DocumentType DetectType(string document)
    {
        string value = Normalize(document);
        if (NieRegex().IsMatch(value))
        {
            return DocumentType.Nie;
        }
        if (DniRegex().IsMatch(value))
        {
            return DocumentType.Dni;
        }
        if (CifRegex().IsMatch(value))
        {
            return DocumentType.Cif;
        }
        return DocumentType.Unknown;
    }

    /// <summary>
    /// Comprueba que el documento tenga formato válido y carácter de control correcto.
    /// </summary>
    public static bool IsValid(string document, out DocumentType type)
    {
        type = DetectType(document);
        string value = Normalize(document);
        return type switch
        {
            DocumentType.Dni => HasValidDniControl(value),
            DocumentType.Nie => HasValidNieControl(value),
            DocumentType.Cif => HasValidCifControl(value),
            _ => false
        };
    }

    // DNI: la letra de control es el resto del número entre 23.
    private static bool HasValidDniControl(string value)
    {
        Match match = DniRegex().Match(value);
        long number = long.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        return ControlLetters[(int)(number % 23)] == match.Groups[2].Value[0];
    }

    // NIE: se sustituye X/Y/Z por 0/1/2 y se aplica el mismo algoritmo que el DNI.
    private static bool HasValidNieControl(string value)
    {
        Match match = NieRegex().Match(value);
        int prefix = match.Groups[1].Value[0] switch
        {
            'X' => 0,
            'Y' => 1,
            _ => 2
        };
        long number = long.Parse(FormattableString.Invariant($"{prefix}{match.Groups[2].Value}"), CultureInfo.InvariantCulture);
        return ControlLetters[(int)(number % 23)] == match.Groups[3].Value[0];
    }

    // CIF: dígito o letra de control según el tipo de organización.
    private static bool HasValidCifControl(string value)
    {
        Match match = CifRegex().Match(value);
        char organization = match.Groups[1].Value[0];
        string digits = match.Groups[2].Value;
        char control = match.Groups[3].Value[0];

        // Posiciones impares (1, 3, 5 y 7): se duplican y se suman sus dígitos.
        // Posiciones pares (2, 4 y 6): se suman directamente.
        int evenSum = 0;
        int oddSum = 0;
        for (int i = 0; i < digits.Length; i++)
        {
            int digit = digits[i] - '0';
            if ((i + 1) % 2 == 0)
            {
                evenSum += digit;
            }
            else
            {
                int doubled = digit * 2;
                oddSum += doubled / 10 + doubled % 10;
            }
        }

        int controlDigit = (10 - ((evenSum + oddSum) % 10)) % 10;
        char controlLetter = CifControlLetters[controlDigit];

        if (CifLetterControlOrganizations.Contains(organization))
        {
            return control == controlLetter;
        }
        if (CifDigitControlOrganizations.Contains(organization))
        {
            return control == (char)('0' + controlDigit);
        }

        // El resto de organizaciones admite dígito o letra.
        return control == (char)('0' + controlDigit) || control == controlLetter;
    }
}
