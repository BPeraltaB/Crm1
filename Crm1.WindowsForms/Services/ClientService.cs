using System.Text;
using System.Text.RegularExpressions;
using Crm1.WindowsForms.Models;

namespace Crm1.WindowsForms.Services;

/// <summary>
/// Reglas de negocio del módulo de clientes. Viven fuera de los formularios
/// (constitución, punto 3). Los mensajes de error son cortos y van dirigidos a la
/// celda correspondiente (specs/002, sección 4.1).
/// </summary>
public sealed partial class ClientService
{
    // Móvil español: 9 dígitos que empiezan por 6 o 7, con prefijo +34 opcional.
    [GeneratedRegex(@"^(\+34)?[67]\d{8}$")]
    private static partial Regex MobilePhoneRegex();

    // Correo electrónico: forma local@dominio con al menos un punto en el dominio.
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    private readonly PostalCodeCatalog _catalog;

    /// <summary>
    /// Crea el servicio con el catálogo de códigos postales que usan las reglas de
    /// ubicación (CP↔ciudad y ciudad↔provincia).
    /// </summary>
    public ClientService(PostalCodeCatalog catalog)
    {
        _catalog = catalog;
    }

    /// <summary>
    /// Valida un cliente y devuelve los errores encontrados, campo a campo.
    /// </summary>
    public IReadOnlyList<ClientValidationError> ValidateClient(Client client)
    {
        var errors = new List<ClientValidationError>();

        // Normaliza los valores antes de validar.
        string document = SpanishDocuments.Normalize(client.TaxId);
        string phone = NormalizePhone(client.Phone);
        DocumentType documentType = SpanishDocuments.DetectType(document);

        if (string.IsNullOrWhiteSpace(client.FirstName))
        {
            errors.Add(new() { Field = nameof(Client.FirstName), Message = "La celda está vacía." });
        }

        if (string.IsNullOrWhiteSpace(document))
        {
            errors.Add(new() { Field = nameof(Client.TaxId), Message = "La celda está vacía." });
        }
        else if (!SpanishDocuments.IsValid(document, out _))
        {
            errors.Add(new() { Field = nameof(Client.TaxId), Message = "El documento no es válido." });
        }

        // El primer apellido solo puede quedar vacío con un CIF (empresas).
        if (string.IsNullOrWhiteSpace(client.LastName1) && documentType != DocumentType.Cif)
        {
            errors.Add(new() { Field = nameof(Client.LastName1), Message = "La celda está vacía." });
        }

        // El segundo apellido solo puede quedar vacío con un CIF o un NIE.
        if (string.IsNullOrWhiteSpace(client.LastName2)
            && documentType != DocumentType.Cif && documentType != DocumentType.Nie)
        {
            errors.Add(new() { Field = nameof(Client.LastName2), Message = "La celda está vacía." });
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            errors.Add(new() { Field = nameof(Client.Phone), Message = "La celda está vacía." });
        }
        else if (!MobilePhoneRegex().IsMatch(phone))
        {
            errors.Add(new() { Field = nameof(Client.Phone), Message = "El teléfono no es un móvil válido." });
        }

        if (string.IsNullOrWhiteSpace(client.Email))
        {
            errors.Add(new() { Field = nameof(Client.Email), Message = "La celda está vacía." });
        }
        else if (!EmailRegex().IsMatch(client.Email.Trim()))
        {
            errors.Add(new() { Field = nameof(Client.Email), Message = "El correo electrónico no es válido." });
        }

        if (string.IsNullOrWhiteSpace(client.Address))
        {
            errors.Add(new() { Field = nameof(Client.Address), Message = "La celda está vacía." });
        }

        if (string.IsNullOrWhiteSpace(client.Province))
        {
            errors.Add(new() { Field = nameof(Client.Province), Message = "La celda está vacía." });
        }

        if (client.RegistrationDate == default)
        {
            errors.Add(new() { Field = nameof(Client.RegistrationDate), Message = "La celda está vacía." });
        }

        ValidatePostalCodeAndLocation(client, errors);

        return errors;
    }

    /// <summary>
    /// Valida todos los clientes de la tabla y comprueba que el documento no se
    /// repita entre ellos. Los índices de fila corresponden a la lista recibida.
    /// </summary>
    public IReadOnlyList<RowValidationError> ValidateClients(IReadOnlyList<Client> clients)
    {
        var errors = new List<RowValidationError>();

        for (int i = 0; i < clients.Count; i++)
        {
            foreach (ClientValidationError error in ValidateClient(clients[i]))
            {
                errors.Add(new() { RowIndex = i, Error = error });
            }
        }

        // El documento debe ser único entre los clientes de la tabla.
        var duplicated = clients
            .Select((client, index) => new { Document = SpanishDocuments.Normalize(client.TaxId), Index = index })
            .Where(item => !string.IsNullOrWhiteSpace(item.Document))
            .GroupBy(item => item.Document)
            .Where(group => group.Count() > 1);

        foreach (var group in duplicated)
        {
            foreach (var item in group)
            {
                errors.Add(new()
                {
                    RowIndex = item.Index,
                    Error = new() { Field = nameof(Client.TaxId), Message = "El documento está repetido." }
                });
            }
        }

        return errors;
    }

    /// <summary>
    /// Quita espacios y guiones de un número de teléfono, conservando un +34 si existe.
    /// </summary>
    public static string NormalizePhone(string phone)
    {
        var builder = new StringBuilder();
        foreach (char c in phone.Trim())
        {
            if (char.IsDigit(c) || c == '+')
            {
                builder.Append(c);
            }
        }
        return builder.ToString();
    }

    /// <summary>
    /// Indica si un cliente pasa el filtro del listado (specs/002): el texto
    /// busca en las columnas visibles —coincidencia parcial, sin mayúsculas ni
    /// tildes, como la búsqueda global— y el estado filta cuando se indica.
    /// Un texto vacío con estado null deja pasar todo.
    /// </summary>
    public static bool MatchesFilter(Client client, string? text, ClientStatus? status)
    {
        if (status.HasValue && client.Status != status.Value)
        {
            return false;
        }

        string normalized = SearchService.NormalizeText(text ?? string.Empty);
        if (normalized.Length == 0)
        {
            return true;
        }

        return SearchableValues(client).Any(value =>
            value.Length > 0 && SearchService.NormalizeText(value).Contains(normalized, StringComparison.Ordinal));
    }

    // Valores visibles de un cliente en el orden de las columnas de la tabla
    // (los mismos campos que la búsqueda global, specs/004).
    private static IEnumerable<string> SearchableValues(Client client)
    {
        yield return client.FirstName;
        yield return client.LastName1;
        yield return client.LastName2;
        yield return client.TaxId;
        yield return client.Phone;
        yield return client.Email;
        yield return client.Address;
        yield return client.PostalCode;
        yield return client.City;
        yield return client.Province;
        yield return client.Status == ClientStatus.Inactive ? "Inactivo" : "Activo";
        yield return client.RegistrationDate.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
        yield return client.Notes;
    }

    // Valida el código postal (5 dígitos, existente) y su correspondencia con la
    // ciudad y la provincia (reglas 4 y 5).
    private void ValidatePostalCodeAndLocation(Client client, List<ClientValidationError> errors)
    {
        string postalCode = client.PostalCode.Trim();

        if (string.IsNullOrWhiteSpace(postalCode))
        {
            errors.Add(new() { Field = nameof(Client.PostalCode), Message = "La celda está vacía." });
            return;
        }

        if (postalCode.Length != 5 || !postalCode.All(char.IsDigit))
        {
            errors.Add(new() { Field = nameof(Client.PostalCode), Message = "El código postal debe tener 5 dígitos." });
            return;
        }

        if (!_catalog.Exists(postalCode))
        {
            errors.Add(new() { Field = nameof(Client.PostalCode), Message = "El código postal no existe." });
            return;
        }

        if (string.IsNullOrWhiteSpace(client.City))
        {
            errors.Add(new() { Field = nameof(Client.City), Message = "La celda está vacía." });
            return;
        }

        // Regla 5: la ciudad debe pertenecer a la provincia seleccionada.
        if (!string.IsNullOrWhiteSpace(client.Province)
            && !_catalog.CityBelongsToProvince(client.City, client.Province))
        {
            errors.Add(new() { Field = nameof(Client.City), Message = "La ciudad no pertenece a la provincia." });
            return;
        }

        // Regla 4: el código postal debe corresponder con la ciudad (solo si el
        // catálogo tiene municipios para ese código).
        var municipalities = _catalog.GetMunicipalities(postalCode);
        if (municipalities.Count > 0 && !_catalog.CityMatchesPostalCode(client.City, postalCode))
        {
            errors.Add(new() { Field = nameof(Client.PostalCode), Message = "El código postal no corresponde a la ciudad." });
        }
    }
}
