using System.Globalization;
using System.Text;
using Crm1.WindowsForms.Models;

namespace Crm1.WindowsForms.Services;

/// <summary>
/// Módulo al que pertenece un resultado de la búsqueda global.
/// </summary>
public enum SearchModule
{
    /// <summary>Cliente registrado (tabla de ClientForm).</summary>
    Client,

    /// <summary>Futuro cliente (tabla de ContactForm).</summary>
    Contact
}

/// <summary>
/// Coincidencia de la búsqueda global: un registro con todos los campos que
/// han coincidido con el término buscado (specs/004-busqueda-global.md).
/// </summary>
public sealed class SearchMatch
{
    /// <summary>Módulo al que pertenece el registro encontrado.</summary>
    public SearchModule Module { get; init; }

    /// <summary>Identificador del registro, para navegar hasta él.</summary>
    public Guid Id { get; init; }

    /// <summary>Nombre para mostrar: nombre y apellidos no vacíos.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Campos coincidentes agrupados ("Ciudad: Madrid; Nombre: Ana").</summary>
    public string MatchedText { get; init; } = string.Empty;

    /// <summary>Registro completo encontrado cuando es un cliente.</summary>
    public Client? Client { get; init; }

    /// <summary>Registro completo encontrado cuando es un futuro cliente.</summary>
    public Contact? Contact { get; init; }
}

/// <summary>
/// Búsqueda global sobre clientes y futuros clientes: coincidencia total o
/// parcial, sin mayúsculas ni acentos, en los campos visibles de las tablas
/// (specs/004). Lógica fuera de los formularios (constitución, punto 3).
/// </summary>
public static class SearchService
{
    /// <summary>
    /// Devuelve las coincidencias: primero clientes y luego futuros clientes,
    /// en el orden de las listas. Un registro ocupa una fila con todos sus
    /// campos coincidentes agrupados; un término vacío no devuelve nada.
    /// </summary>
    public static IReadOnlyList<SearchMatch> Search(IEnumerable<Client> clients, IEnumerable<Contact> contacts, string? term)
    {
        var matches = new List<SearchMatch>();
        string normalizedTerm = NormalizeText(term ?? string.Empty);
        if (normalizedTerm.Length == 0)
        {
            return matches;
        }

        foreach (Client client in clients)
        {
            AddMatch(matches, SearchModule.Client, client.Id,
                client, null,
                BuildDisplayName(client.FirstName, client.LastName1, client.LastName2),
                ClientFields(client),
                normalizedTerm);
        }

        foreach (Contact contact in contacts)
        {
            AddMatch(matches, SearchModule.Contact, contact.Id,
                null, contact,
                BuildDisplayName(contact.FirstName, contact.LastName1, contact.LastName2),
                ContactFields(contact),
                normalizedTerm);
        }

        return matches;
    }

    // Añade una coincidencia si algún campo del registro contiene el término.
    private static void AddMatch(
        List<SearchMatch> matches,
        SearchModule module,
        Guid id,
        Client? client,
        Contact? contact,
        string displayName,
        (string Label, string Value)[] fields,
        string normalizedTerm)
    {
        var matched = new List<string>();
        foreach ((string label, string value) in fields)
        {
            if (value.Length > 0 && NormalizeText(value).Contains(normalizedTerm, StringComparison.Ordinal))
            {
                matched.Add($"{label}: {value}");
            }
        }

        if (matched.Count > 0)
        {
            matches.Add(new SearchMatch
            {
                Module = module,
                Id = id,
                DisplayName = displayName,
                MatchedText = string.Join("; ", matched),
                Client = client,
                Contact = contact
            });
        }
    }

    // Campos visibles de un cliente, en el orden de las columnas de la tabla.
    private static (string Label, string Value)[] ClientFields(Client client) => new[]
    {
        ("Nombre", client.FirstName),
        ("Primer apellido", client.LastName1),
        ("Segundo apellido", client.LastName2),
        ("Documento", client.TaxId),
        ("Teléfono", client.Phone),
        ("Correo electrónico", client.Email),
        ("Dirección", client.Address),
        ("Código postal", client.PostalCode),
        ("Ciudad", client.City),
        ("Provincia", client.Province),
        ("Estado", client.Status == ClientStatus.Inactive ? "Inactivo" : "Activo"),
        ("Fecha de alta", client.RegistrationDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)),
        ("Observaciones", client.Notes)
    };

    // Campos visibles de un futuro cliente; la fecha vacía no se busca.
    private static (string Label, string Value)[] ContactFields(Contact contact) => new[]
    {
        ("Nombre", contact.FirstName),
        ("Primer apellido", contact.LastName1),
        ("Segundo apellido", contact.LastName2),
        ("Documento", contact.TaxId),
        ("Teléfono", contact.Phone),
        ("Correo electrónico", contact.Email),
        ("Dirección", contact.Address),
        ("Código postal", contact.PostalCode),
        ("Ciudad", contact.City),
        ("Provincia", contact.Province),
        ("Estado", contact.Status == ContactStatus.Discarded ? "Descartado" : "Pendiente"),
        ("Fecha de alta", contact.RegistrationDate == default
            ? string.Empty
            : contact.RegistrationDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)),
        ("Observaciones", contact.Notes)
    };

    // Nombre para mostrar: nombre y apellidos no vacíos (razón social en empresas).
    private static string BuildDisplayName(string firstName, string lastName1, string lastName2) =>
        string.Join(" ", new[] { firstName, lastName1, lastName2 }.Where(part => part.Length > 0));

    // Normaliza para comparar: recorte, minúsculas y sin tildes ("Málaga" = "malaga").
    // Pública para que los filtros de los módulos comparen igual que la búsqueda.
    public static string NormalizeText(string text)
    {
        string trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(trimmed.Length);
        string decomposed = trimmed.Normalize(NormalizationForm.FormD);
        foreach (char c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}
