using System.Text.RegularExpressions;
using Crm1.WindowsForms.Models;

namespace Crm1.WindowsForms.Services;

/// <summary>
/// Reglas de negocio del módulo de futuros clientes (specs/003). Viven fuera de
/// los formularios (constitución, punto 3). A diferencia de los clientes, las
/// celdas pueden quedar vacías —no siempre se conocen todos los datos— salvo
/// el nombre y un medio de contacto (teléfono o correo). Lo que sí está relleno
/// debe ser correcto.
/// </summary>
public sealed partial class ContactService
{
    // Teléfono español: fijo o móvil, 9 dígitos que empiezan por 6, 7, 8 o 9.
    [GeneratedRegex(@"^(\+34)?[6789]\d{8}$")]
    private static partial Regex SpanishPhoneRegex();

    // Correo electrónico: forma local@dominio con al menos un punto en el dominio.
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    private readonly PostalCodeCatalog _catalog;
    private readonly HashSet<string> _clientDocuments;
    private readonly ClientService _clientService;

    /// <summary>
    /// Crea el servicio con el catálogo de códigos postales y los documentos de
    /// los clientes ya guardados: un documento que pertenece a un cliente no
    /// puede registrarse como futuro cliente (specs/003).
    /// </summary>
    public ContactService(PostalCodeCatalog catalog, IEnumerable<string> clientDocuments)
    {
        _catalog = catalog;
        _clientDocuments = new HashSet<string>(clientDocuments
            .Select(SpanishDocuments.Normalize)
            .Where(document => !string.IsNullOrWhiteSpace(document)));
        _clientService = new ClientService(catalog);
    }

    /// <summary>
    /// Valida un futuro cliente y devuelve los errores encontrados, campo a
    /// campo. Las celdas vacías son válidas salvo el nombre y el medio de
    /// contacto.
    /// </summary>
    public IReadOnlyList<ContactValidationError> ValidateContact(Contact contact)
    {
        var errors = new List<ContactValidationError>();

        // Normaliza los valores antes de validar.
        string document = SpanishDocuments.Normalize(contact.TaxId);
        string phone = ClientService.NormalizePhone(contact.Phone);

        // El nombre es el único campo obligatorio.
        if (string.IsNullOrWhiteSpace(contact.FirstName))
        {
            errors.Add(new() { Field = nameof(Contact.FirstName), Message = "La celda está vacía." });
        }

        // Hay que conocer al menos un medio de contacto: teléfono o correo.
        if (string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(contact.Email))
        {
            errors.Add(new() { Field = nameof(Contact.Phone), Message = "Debes indicar un teléfono o un correo electrónico." });
            errors.Add(new() { Field = nameof(Contact.Email), Message = "Debes indicar un teléfono o un correo electrónico." });
        }

        // Teléfono relleno: fijo o móvil español.
        if (!string.IsNullOrWhiteSpace(phone) && !SpanishPhoneRegex().IsMatch(phone))
        {
            errors.Add(new() { Field = nameof(Contact.Phone), Message = "El teléfono no es válido." });
        }

        // Correo relleno: formato válido.
        if (!string.IsNullOrWhiteSpace(contact.Email) && !EmailRegex().IsMatch(contact.Email.Trim()))
        {
            errors.Add(new() { Field = nameof(Contact.Email), Message = "El correo electrónico no es válido." });
        }

        // Documento relleno: DNI, NIE o CIF con su carácter de control correcto.
        if (!string.IsNullOrWhiteSpace(document) && !SpanishDocuments.IsValid(document, out _))
        {
            errors.Add(new() { Field = nameof(Contact.TaxId), Message = "El documento no es válido." });
        }

        ValidatePostalCodeAndLocation(contact, errors);

        return errors;
    }

    /// <summary>
    /// Valida todos los futuros clientes de la tabla, comprueba que el documento
    /// no se repita entre ellos y que no pertenezca a un cliente guardado. Los
    /// índices de fila corresponden a la lista recibida.
    /// </summary>
    public IReadOnlyList<ContactRowValidationError> ValidateContacts(IReadOnlyList<Contact> contacts)
    {
        var errors = new List<ContactRowValidationError>();

        for (int i = 0; i < contacts.Count; i++)
        {
            foreach (ContactValidationError error in ValidateContact(contacts[i]))
            {
                errors.Add(new() { RowIndex = i, Error = error });
            }
        }

        // El documento debe ser único entre los futuros clientes de la tabla.
        var duplicated = contacts
            .Select((contact, index) => new { Document = SpanishDocuments.Normalize(contact.TaxId), Index = index })
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
                    Error = new() { Field = nameof(Contact.TaxId), Message = "El documento está repetido." }
                });
            }
        }

        // Un documento registrado en clientes no puede ser futuro cliente.
        foreach (var item in contacts
            .Select((contact, index) => new { Document = SpanishDocuments.Normalize(contact.TaxId), Index = index })
            .Where(item => !string.IsNullOrWhiteSpace(item.Document) && _clientDocuments.Contains(item.Document)))
        {
            errors.Add(new()
            {
                RowIndex = item.Index,
                Error = new() { Field = nameof(Contact.TaxId), Message = "El documento ya está registrado en clientes." }
            });
        }

        return errors;
    }

    // Valida el código postal (5 dígitos, existente) y su correspondencia con la
    // ciudad y la provincia; cada regla solo se aplica si los campos implicados
    // están rellenos (specs/003).
    private void ValidatePostalCodeAndLocation(Contact contact, List<ContactValidationError> errors)
    {
        string postalCode = contact.PostalCode.Trim();
        string city = contact.City.Trim();

        // Sin código postal no hay nada que validar de ubicación.
        if (!string.IsNullOrWhiteSpace(postalCode))
        {
            if (postalCode.Length != 5 || !postalCode.All(char.IsDigit))
            {
                errors.Add(new() { Field = nameof(Contact.PostalCode), Message = "El código postal debe tener 5 dígitos." });
                return;
            }

            if (!_catalog.Exists(postalCode))
            {
                errors.Add(new() { Field = nameof(Contact.PostalCode), Message = "El código postal no existe." });
                return;
            }

            // El CP debe corresponder con la ciudad, si esta se conoce.
            if (!string.IsNullOrWhiteSpace(city)
                && _catalog.GetMunicipalities(postalCode).Count > 0
                && !_catalog.CityMatchesPostalCode(city, postalCode))
            {
                errors.Add(new() { Field = nameof(Contact.PostalCode), Message = "El código postal no corresponde a la ciudad." });
            }
        }

        // La ciudad debe pertenecer a la provincia, si ambas se conocen.
        if (!string.IsNullOrWhiteSpace(city)
            && !string.IsNullOrWhiteSpace(contact.Province)
            && !_catalog.CityBelongsToProvince(city, contact.Province))
        {
            errors.Add(new() { Field = nameof(Contact.City), Message = "La ciudad no pertenece a la provincia." });
        }
    }

    /// <summary>
    /// Indica si un futuro cliente pasa el filtro del listado (specs/003): el
    /// texto busca en las columnas visibles —coincidencia parcial, sin
    /// mayúsculas ni tildes, como la búsqueda global— y el estado filtra cuando
    /// se indica. Un texto vacío con estado null deja pasar todo.
    /// </summary>
    public static bool MatchesFilter(Contact contact, string? text, ContactStatus? status)
    {
        if (status.HasValue && contact.Status != status.Value)
        {
            return false;
        }

        string normalized = SearchService.NormalizeText(text ?? string.Empty);
        if (normalized.Length == 0)
        {
            return true;
        }

        return SearchableValues(contact).Any(value =>
            value.Length > 0 && SearchService.NormalizeText(value).Contains(normalized, StringComparison.Ordinal));
    }

    /// <summary>
    /// Convierte un futuro cliente en cliente (specs/003, sección 7): copia los
    /// datos, comprueba que cumplan TODAS las reglas de los clientes y devuelve
    /// el cliente listo (estado Activo, identificador nuevo) o la lista de
    /// carencias que impiden la conversión.
    /// </summary>
    public ConversionResult ConvertToClient(Contact contact)
    {
        // El cliente nace Activo y conserva la fecha del futuro cliente; sin
        // fecha no puede darse de alta (regla de clientes) y quedará como carencia.
        var candidate = new Client
        {
            Id = Guid.NewGuid(),
            FirstName = contact.FirstName.Trim(),
            LastName1 = contact.LastName1.Trim(),
            LastName2 = contact.LastName2.Trim(),
            TaxId = SpanishDocuments.Normalize(contact.TaxId),
            Phone = ClientService.NormalizePhone(contact.Phone),
            Email = contact.Email.Trim(),
            Address = contact.Address.Trim(),
            PostalCode = contact.PostalCode.Trim(),
            City = contact.City.Trim(),
            Province = contact.Province.Trim(),
            Status = ClientStatus.Active,
            RegistrationDate = contact.RegistrationDate,
            Notes = contact.Notes.Trim()
        };

        // Validación completa con las reglas de los clientes.
        var errors = new List<string>();
        foreach (ClientValidationError error in _clientService.ValidateClient(candidate))
        {
            errors.Add($"{FieldLabel(error.Field)}: {ConversionHint(error)}");
        }

        return errors.Count > 0
            ? new ConversionResult { Errors = errors }
            : new ConversionResult { Client = candidate };
    }

    // Mensaje orientado a la conversión a partir del error de validación de clientes.
    private static string ConversionHint(ClientValidationError error) => error.Field switch
    {
        nameof(Client.Phone) => "los clientes exigen un móvil que empiece por 6 o 7.",
        nameof(Client.RegistrationDate) => "los clientes necesitan fecha de alta (dd/mm/aaaa).",
        nameof(Client.LastName1) => "falta el primer apellido (obligatorio salvo empresas con CIF).",
        nameof(Client.LastName2) => "falta el segundo apellido (obligatorio salvo CIF o NIE).",
        _ => error.Message
    };

    // Nombre en español del campo, para la lista de carencias del diálogo.
    private static string FieldLabel(string field) => field switch
    {
        nameof(Client.FirstName) => "Nombre",
        nameof(Client.LastName1) => "Primer apellido",
        nameof(Client.LastName2) => "Segundo apellido",
        nameof(Client.TaxId) => "Documento",
        nameof(Client.Phone) => "Teléfono",
        nameof(Client.Email) => "Correo electrónico",
        nameof(Client.Address) => "Dirección",
        nameof(Client.PostalCode) => "Código postal",
        nameof(Client.City) => "Ciudad",
        nameof(Client.Province) => "Provincia",
        nameof(Client.RegistrationDate) => "Fecha de alta",
        _ => field
    };

    // Valores visibles de un futuro cliente en el orden de las columnas de la
    // tabla; la fecha vacía no se busca (igual que la búsqueda global).
    private static IEnumerable<string> SearchableValues(Contact contact)
    {
        yield return contact.FirstName;
        yield return contact.LastName1;
        yield return contact.LastName2;
        yield return contact.TaxId;
        yield return contact.Phone;
        yield return contact.Email;
        yield return contact.Address;
        yield return contact.PostalCode;
        yield return contact.City;
        yield return contact.Province;
        yield return contact.Status == ContactStatus.Discarded ? "Descartado" : "Pendiente";
        yield return contact.RegistrationDate == default
            ? string.Empty
            : contact.RegistrationDate.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
        yield return contact.Notes;
    }
}
