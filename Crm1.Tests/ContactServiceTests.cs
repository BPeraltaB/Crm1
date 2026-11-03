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
}
