using System.Text;
using Crm1.WindowsForms.Models;
using Crm1.WindowsForms.Services;
using Xunit;

namespace Crm1.Tests;

/// <summary>
/// Pruebas de las reglas de negocio de futuros clientes (specs/003): celdas
/// vacías permitidas salvo el nombre y un medio de contacto. Usan un catálogo
/// de códigos postales reducido propio.
/// </summary>
public sealed class ContactServiceTests : IDisposable
{
    private readonly string _catalogPath;
    private readonly ContactService _service;

    public ContactServiceTests()
    {
        _catalogPath = Path.Combine(
            Path.GetTempPath(), "opencode", "crm1-tests", $"contacts-catalog-{Guid.NewGuid():N}.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_catalogPath)!);
        File.WriteAllText(_catalogPath,
            "[{\"postalCode\":\"01001\",\"municipalities\":[\"Vitoria-Gasteiz\"]}," +
            "{\"postalCode\":\"01010\",\"municipalities\":[\"Alegría-Dulantzi\"]}," +
            "{\"postalCode\":\"28001\",\"municipalities\":[\"Madrid\"]}," +
            "{\"postalCode\":\"28002\",\"municipalities\":[\"Madrid\"]}]",
            Encoding.UTF8);

        // Un CIF de empresa ya registrado como cliente para la regla cruzada.
        _service = new ContactService(new PostalCodeCatalog(_catalogPath), new[] { "A46103834" });
    }

    public void Dispose()
    {
        File.Delete(_catalogPath);
    }

    // Futuro cliente válido mínimo: nombre y un teléfono; nada más.
    private static Contact ValidContact(Action<Contact>? tweak = null)
    {
        var contact = new Contact
        {
            FirstName = "Lucía",
            Phone = "612345678"
        };
        tweak?.Invoke(contact);
        return contact;
    }

    [Fact]
    public void Valid_minimal_contact_has_no_errors()
    {
        Assert.Empty(_service.ValidateContact(ValidContact()));
    }

    [Fact]
    public void Valid_complete_contact_has_no_errors()
    {
        var contact = ValidContact(c =>
        {
            c.LastName1 = "Fernández";
            c.LastName2 = "Ruiz";
            c.TaxId = "12345678Z";
            c.Address = "Calle Mayor 1";
            c.PostalCode = "28001";
            c.City = "Madrid";
            c.Province = "Madrid";
            c.Phone = "912345678";
            c.Email = "lucia@example.com";
            c.RegistrationDate = new DateTime(2026, 4, 5);
            c.Notes = "Visto en una feria";
            c.Status = ContactStatus.Discarded;
        });

        Assert.Empty(_service.ValidateContact(contact));
    }

    [Fact]
    public void Name_is_required()
    {
        var errors = _service.ValidateContact(ValidContact(c => c.FirstName = " "));
        Assert.Contains(errors, e => e.Field == nameof(Contact.FirstName));
    }

    [Fact]
    public void Missing_phone_and_email_marks_both_cells()
    {
        var errors = _service.ValidateContact(ValidContact(c => c.Phone = ""));

        Assert.Contains(errors, e => e.Field == nameof(Contact.Phone)
            && e.Message.Contains("teléfono o un correo electrónico"));
        Assert.Contains(errors, e => e.Field == nameof(Contact.Email)
            && e.Message.Contains("teléfono o un correo electrónico"));
    }

    [Fact]
    public void Email_alone_is_enough_contact_data()
    {
        // Sin teléfono ni correo hay error; con solo el correo, todo correcto.
        var withoutBoth = _service.ValidateContact(ValidContact(c => c.Phone = ""));
        Assert.Contains(withoutBoth, e => e.Field == nameof(Contact.Phone));
        Assert.Contains(withoutBoth, e => e.Field == nameof(Contact.Email));

        var withEmailOnly = _service.ValidateContact(ValidContact(c =>
        {
            c.Phone = "";
            c.Email = "lucia@example.com";
        }));
        Assert.Empty(withEmailOnly);
    }

    [Theory]
    [InlineData("912345678", true)]       // fijo: empieza por 9
    [InlineData("812345678", true)]       // fijo: empieza por 8
    [InlineData("612345678", true)]       // móvil
    [InlineData("+34912345678", true)]    // fijo con prefijo
    [InlineData("612 34 56 78", true)]    // con espacios (se normalizan)
    [InlineData("", true)]                // vacío permitido (si hay correo)
    [InlineData("123456789", false)]      // empieza por 1
    [InlineData("61234567", false)]       // 8 dígitos
    public void Phone_accepts_any_spanish_phone_when_filled(string phone, bool isValid)
    {
        var errors = _service.ValidateContact(ValidContact(c =>
        {
            c.Phone = phone;
            c.Email = "lucia@example.com";
        }));

        Assert.Equal(isValid, errors.All(e => e.Field != nameof(Contact.Phone)));
    }

    [Theory]
    [InlineData("", true)]                 // vacío permitido (si hay teléfono)
    [InlineData("lucia.example.com", false)]
    [InlineData("lucia@ejemplo", false)]   // sin punto en el dominio
    [InlineData("lucia@example.com", true)]
    public void Email_when_filled_must_be_well_formed(string email, bool isValid)
    {
        var errors = _service.ValidateContact(ValidContact(c => c.Email = email));

        Assert.Equal(isValid, errors.All(e => e.Field != nameof(Contact.Email)));
    }

    [Theory]
    [InlineData("", true)]                 // vacío permitido (dato opcional)
    [InlineData("12345678A", false)]       // letra de control incorrecta
    [InlineData("12345678Z", true)]
    public void Document_when_filled_must_be_valid(string taxId, bool isValid)
    {
        var errors = _service.ValidateContact(ValidContact(c => c.TaxId = taxId));

        Assert.Equal(isValid, errors.All(e => e.Field != nameof(Contact.TaxId)));
    }

    [Fact]
    public void Postal_code_when_filled_must_have_five_digits()
    {
        var errors = _service.ValidateContact(ValidContact(c => c.PostalCode = "1234"));
        Assert.Contains(errors, e => e.Field == nameof(Contact.PostalCode) && e.Message.Contains("5 dígitos"));
    }

    [Fact]
    public void Postal_code_when_filled_must_exist()
    {
        var errors = _service.ValidateContact(ValidContact(c => c.PostalCode = "88888"));
        Assert.Contains(errors, e => e.Field == nameof(Contact.PostalCode) && e.Message.Contains("no existe"));
    }

    [Fact]
    public void Postal_code_must_match_the_city_when_both_are_filled()
    {
        // Alegría-Dulantzi está en Araba/Álava, pero el CP 28001 es de Madrid.
        var errors = _service.ValidateContact(ValidContact(c =>
        {
            c.PostalCode = "28001";
            c.City = "Alegría-Dulantzi";
            c.Province = "Araba/Álava";
        }));
        Assert.Contains(errors, e =>
            e.Field == nameof(Contact.PostalCode) && e.Message.Contains("no corresponde a la ciudad"));
    }

    [Fact]
    public void Postal_code_without_city_is_valid()
    {
        var errors = _service.ValidateContact(ValidContact(c => c.PostalCode = "28001"));
        Assert.Empty(errors);
    }

    [Fact]
    public void City_must_belong_to_the_province_when_both_are_filled()
    {
        var errors = _service.ValidateContact(ValidContact(c =>
        {
            c.City = "Madrid";
            c.Province = "Barcelona";
        }));
        Assert.Contains(errors, e =>
            e.Field == nameof(Contact.City) && e.Message.Contains("no pertenece a la provincia"));
    }

    [Fact]
    public void City_without_province_is_valid()
    {
        var errors = _service.ValidateContact(ValidContact(c => c.City = "Madrid"));
        Assert.Empty(errors);
    }

    [Fact]
    public void Document_must_be_unique_in_the_table()
    {
        Contact first = ValidContact(c => c.TaxId = "12345678Z");
        Contact second = ValidContact(c => { c.FirstName = "Pedro"; c.TaxId = "12345678Z"; });

        var errors = _service.ValidateContacts(new[] { first, second });

        Assert.Equal(2, errors.Count(e => e.Error.Field == nameof(Contact.TaxId)
            && e.Error.Message.Contains("repetido")));
    }

    [Fact]
    public void Document_of_an_existing_client_cannot_be_a_contact()
    {
        var errors = _service.ValidateContacts(new[] { ValidContact(c => c.TaxId = "A46103834") });

        ContactRowValidationError error = Assert.Single(errors);
        Assert.Equal(nameof(Contact.TaxId), error.Error.Field);
        Assert.Contains("ya está registrado en clientes", error.Error.Message);
        Assert.Equal(0, error.RowIndex);
    }

    // Pruebas del filtro del listado (specs/003, búsqueda y filtro).

    [Fact]
    public void Contact_filter_with_empty_text_and_no_status_returns_everything()
    {
        Assert.True(ContactService.MatchesFilter(ValidContact(), "  ", null));
    }

    [Fact]
    public void Contact_filter_matches_without_accents()
    {
        Contact contact = ValidContact(c => c.LastName1 = "Fernández");
        Assert.True(ContactService.MatchesFilter(contact, "fernandez", null));
    }

    [Fact]
    public void Contact_filter_by_status_excludes_the_other_state()
    {
        Contact pending = ValidContact();
        Contact discarded = ValidContact(c => c.Status = ContactStatus.Discarded);

        Assert.True(ContactService.MatchesFilter(pending, null, ContactStatus.Pending));
        Assert.False(ContactService.MatchesFilter(discarded, null, ContactStatus.Pending));
        Assert.True(ContactService.MatchesFilter(discarded, null, ContactStatus.Discarded));
    }

    [Fact]
    public void Contact_without_date_does_not_match_date_searches()
    {
        // La fecha vacía de un futuro cliente no se busca (igual que en la búsqueda global).
        Assert.False(ContactService.MatchesFilter(ValidContact(), "15/01/2026", null));
    }

    // Pruebas de la conversión de futuro cliente en cliente (specs/003, sección 7).

    // Futuro cliente que ya cumple todas las reglas de los clientes.
    private static Contact ConvertibleContact(Action<Contact>? tweak = null)
    {
        var contact = new Contact
        {
            FirstName = "Lucía",
            LastName1 = "Fernández",
            LastName2 = "Ruiz",
            TaxId = "12345678Z",
            Address = "Calle Mayor 1",
            PostalCode = "28001",
            City = "Madrid",
            Province = "Madrid",
            Phone = "612345678",
            Email = "lucia@example.com",
            RegistrationDate = new DateTime(2026, 4, 5),
            Notes = "Visto en una feria"
        };
        tweak?.Invoke(contact);
        return contact;
    }

    [Fact]
    public void Convert_complete_contact_succeeds_as_active_client()
    {
        ConversionResult result = _service.ConvertToClient(ConvertibleContact());

        Assert.True(result.Success);
        Assert.Equal("Lucía", result.Client!.FirstName);
        Assert.Equal(ClientStatus.Active, result.Client.Status);
        Assert.NotEqual(Guid.Empty, result.Client.Id);
        Assert.Equal(new DateTime(2026, 4, 5), result.Client.RegistrationDate);
    }

    [Fact]
    public void Convert_contact_with_landline_phone_fails()
    {
        // El fijo 912345678 vale para un futuro cliente, pero el cliente exige móvil.
        ConversionResult result = _service.ConvertToClient(ConvertibleContact(c => c.Phone = "912345678"));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("Teléfono") && e.Contains("6 o 7"));
    }

    [Fact]
    public void Convert_contact_without_document_fails()
    {
        ConversionResult result = _service.ConvertToClient(ConvertibleContact(c => c.TaxId = ""));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.StartsWith("Documento"));
    }

    [Fact]
    public void Convert_contact_without_registration_date_fails()
    {
        ConversionResult result = _service.ConvertToClient(ConvertibleContact(c => c.RegistrationDate = default));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.StartsWith("Fecha de alta"));
    }

    [Fact]
    public void Convert_contact_without_location_fails()
    {
        ConversionResult result = _service.ConvertToClient(ConvertibleContact(c =>
        {
            c.Address = "";
            c.PostalCode = "";
            c.City = "";
            c.Province = "";
        }));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.StartsWith("Dirección"));
    }

    [Fact]
    public void Convert_contact_with_dni_and_missing_second_surname_fails()
    {
        ConversionResult result = _service.ConvertToClient(ConvertibleContact(c => c.LastName2 = ""));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.StartsWith("Segundo apellido"));
    }

    [Fact]
    public void Convert_company_contact_with_cif_and_empty_surnames_succeeds()
    {
        ConversionResult result = _service.ConvertToClient(ConvertibleContact(c =>
        {
            c.FirstName = "Frutas del Sur SL";
            c.LastName1 = "";
            c.LastName2 = "";
            c.TaxId = "B56789019"; // CIF válido con dígito de control correcto.
        }));

        Assert.True(result.Success);
        Assert.Equal("Frutas del Sur SL", result.Client!.FirstName);
    }
}
