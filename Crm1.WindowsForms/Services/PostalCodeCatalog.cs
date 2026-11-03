using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Crm1.WindowsForms.Services;

/// <summary>
/// Catálogo de códigos postales de España cargado desde un archivo JSON local.
/// Relaciona cada código postal con sus municipios y con su provincia.
/// </summary>
public sealed partial class PostalCodeCatalog
{
    // Nombres oficiales (INE) de las 52 provincias, indexadas por el prefijo de dos
    // dígitos del código postal (01 = Araba/Álava ... 52 = Melilla).
    private static readonly string[] ProvinceNames =
    {
        "Araba/Álava", "Albacete", "Alicante/Alacant", "Almería", "Ávila", "Badajoz",
        "Illes Balears", "Barcelona", "Burgos", "Cáceres", "Cádiz", "Castellón/Castelló",
        "Ciudad Real", "Córdoba", "A Coruña", "Cuenca", "Girona", "Granada",
        "Guadalajara", "Gipuzkoa", "Huelva", "Huesca", "Jaén", "León", "Lleida",
        "La Rioja", "Lugo", "Madrid", "Málaga", "Murcia", "Navarra", "Ourense",
        "Asturias", "Palencia", "Las Palmas", "Pontevedra", "Salamanca",
        "Santa Cruz de Tenerife", "Cantabria", "Segovia", "Sevilla", "Soria",
        "Tarragona", "Teruel", "Toledo", "Valencia/València", "Valladolid", "Bizkaia",
        "Zamora", "Zaragoza", "Ceuta", "Melilla"
    };

    // Deserialización tolerante con el nombre de las propiedades del archivo de datos.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Entrada del archivo de datos (Data/postal-codes-es.json).
    private sealed class PostalCodeEntry
    {
        public string PostalCode { get; set; } = string.Empty;
        public List<string> Municipalities { get; set; } = new();
    }

    private readonly Dictionary<string, List<string>> _postalCodes = new();
    private readonly Dictionary<string, HashSet<string>> _municipalityProvinces = new(StringComparer.Ordinal);

    /// <summary>
    /// Carga el catálogo desde el archivo JSON indicado. Un fallo de lectura o de
    /// formato lanza una excepción visible (constitución, punto 5).
    /// </summary>
    public PostalCodeCatalog(string filePath)
    {
        try
        {
            string json = File.ReadAllText(filePath, Encoding.UTF8);
            List<PostalCodeEntry>? entries = JsonSerializer.Deserialize<List<PostalCodeEntry>>(json, JsonOptions);

            foreach (PostalCodeEntry entry in entries ?? new List<PostalCodeEntry>())
            {
                string code = entry.PostalCode.Trim();
                _postalCodes[code] = entry.Municipalities;

                string? province = GetProvinceName(code);
                if (province is null)
                {
                    continue;
                }

                // Índice municipio → provincias para la regla ciudad↔provincia.
                foreach (string municipality in entry.Municipalities)
                {
                    foreach (string key in NameKeys(municipality))
                    {
                        if (!_municipalityProvinces.TryGetValue(key, out HashSet<string>? provinces))
                        {
                            provinces = new HashSet<string>();
                            _municipalityProvinces[key] = provinces;
                        }
                        provinces.Add(province);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"No se pudo cargar el catálogo de códigos postales '{filePath}'.", ex);
        }
    }

    /// <summary>
    /// Provincias oficiales en orden alfabético español (para los desplegables).
    /// </summary>
    public IReadOnlyList<string> Provinces =>
        ProvinceNames.OrderBy(p => p, StringComparer.CurrentCulture).ToList();

    /// <summary>
    /// Devuelve la provincia de un código postal según su prefijo, o null si el
    /// prefijo no corresponde a ninguna provincia.
    /// </summary>
    public static string? GetProvinceName(string postalCode)
    {
        if (postalCode is null || postalCode.Length < 2)
        {
            return null;
        }

        if (!int.TryParse(postalCode.AsSpan(0, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out int prefix)
            || prefix < 1 || prefix > 52)
        {
            return null;
        }

        return ProvinceNames[prefix - 1];
    }

    /// <summary>
    /// Indica si el código postal existe en el catálogo.
    /// </summary>
    public bool Exists(string postalCode) => _postalCodes.ContainsKey(postalCode.Trim());

    /// <summary>
    /// Devuelve los municipios asociados a un código postal (lista vacía si el
    /// catálogo no tiene municipios para él).
    /// </summary>
    public IReadOnlyList<string> GetMunicipalities(string postalCode) =>
        _postalCodes.TryGetValue(postalCode.Trim(), out List<string>? list)
            ? list
            : new List<string>();

    /// <summary>
    /// Indica si la ciudad pertenece a la provincia indicada (ignora mayúsculas,
    /// tildes y las formas dobles de los nombres oficiales).
    /// </summary>
    public bool CityBelongsToProvince(string city, string province)
    {
        return _municipalityProvinces.TryGetValue(NormalizeName(city), out HashSet<string>? provinces)
            && provinces.Contains(province.Trim());
    }

    /// <summary>
    /// Indica si la ciudad es uno de los municipios del código postal indicado.
    /// </summary>
    public bool CityMatchesPostalCode(string city, string postalCode)
    {
        string key = NormalizeName(city);
        return GetMunicipalities(postalCode).Any(m => NameKeys(m).Contains(key));
    }

    // Claves de búsqueda de un nombre oficial: el nombre completo, cada forma doble
    // (separada por '/' o '-') y el artículo antepuesto ("Molsosa, La" → "La Molsosa").
    private static IEnumerable<string> NameKeys(string name)
    {
        string value = name.Trim();
        yield return NormalizeName(value);

        // Artículo pospuesto: "Coruña, A" → "A Coruña".
        int comma = value.IndexOf(',');
        if (comma > 0 && comma < value.Length - 1)
        {
            string main = value[..comma].Trim();
            string article = value[(comma + 1)..].Trim();
            if (article is "El" or "La" or "Los" or "Las" or "A" or "O" or "L'" or "S'")
            {
                value = $"{article} {main}";
                yield return NormalizeName(value);
            }
        }

        // Formas dobles: "Agurain/Salvatierra", "Alegría-Dulantzi".
        foreach (string part in value.Split('/', '-'))
        {
            string trimmed = part.Trim();
            if (trimmed.Length > 0)
            {
                yield return NormalizeName(trimmed);
            }
        }
    }

    // Normaliza un nombre: minúsculas, sin tildes ni diéresis y con espacios simples.
    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    private static string NormalizeName(string name)
    {
        string formD = name.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (char c in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }
            builder.Append(c);
        }

        return WhitespaceRegex().Replace(builder.ToString().Normalize(NormalizationForm.FormC), " ");
    }
}
