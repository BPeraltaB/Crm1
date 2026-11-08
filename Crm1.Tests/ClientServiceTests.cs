using System.Text;
using Crm1.WindowsForms.Models;
using Crm1.WindowsForms.Services;
using Xunit;

namespace Crm1.Tests;

/// <summary>
/// Pruebas de las reglas de negocio de clientes (una por regla de la spec 000).
/// Usan un catálogo de códigos postales reducido propio.
/// </summary>
public sealed class ClientServiceTests : IDisposable
{
    private readonly string _catalogPath;
    private readonly ClientService _service;

    public ClientServiceTests()
    {
        _catalogPath = Path.Combine(
            Path.GetTempPath(), "opencode", "crm1-tests", $"clients-catalog-{Guid.NewGuid():N}.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_catalogPath)!);
        File.WriteAllText(_catalogPath,
            "[{\"postalCode\":\"01001\",\"municipalities\":[\"Vitoria-Gasteiz\"]}," +
            "{\"postalCode\":\"01010\",\"municipalities\":[\"Alegría-Dulantzi\"]}," +
            "{\"postalCode\":\"28001\",\"municipalities\":[\"Madrid\"]}," +
            "{\"postalCode\":\"28002\",\"municipalities\":[\"Madrid\"]}]",
            Encoding.UTF8);
        _service = new ClientService(new PostalCodeCatalog(_catalogPath));
    }

    public void Dispose()
    {
        File.Delete(_catalogPath);
    }

    // Cliente válido de referencia (DNI, persona con dos apellidos, Madrid).
    private static Client ValidClient(Action<Client>? tweak = null)
    {
        var client = new Client
        {
            FirstName = "Juan",
            LastName1 = "García",
            LastName2 = "López",
            TaxId = "12345678Z",
            Address = "Calle Mayor 1",
            PostalCode = "28001",
            City = "Madrid",
            Province = "Madrid",
            Phone = "612345678",
            Email = "juan@example.com",
            RegistrationDate = new DateTime(2026, 1, 15),
            Notes = "",
            Status = ClientStatus.Active
        };
        tweak?.Invoke(client);
        return client;
    }

    [Fact]
    public void Valid_client_has_no_errors()
    {
        Assert.Empty(_service.ValidateClient(ValidClient()));
    }

    [Fact]
    public void Rule_8_first_name_is_required()
    {
        var errors = _service.ValidateClient(ValidClient(c => c.FirstName = " "));
        Assert.Contains(errors, e => e.Field == nameof(Client.FirstName));
    }

    [Theory]
    [InlineData("12345678A", "no es válido")]  // letra de control incorrecta
    [InlineData("1234", "no es válido")]       // sin formato de documento
    [InlineData("", "vacía")]                  // obligatorio
    public void Rule_1_document_must_be_a_valid_dni_nie_or_cif(string taxId, string expectedWord)
    {
        var errors = _service.ValidateClient(ValidClient(c => c.TaxId = taxId));

        ClientValidationError error = Assert.Single(errors, e => e.Field == nameof(Client.TaxId));
        Assert.Contains(expectedWord, error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rule_6_first_surname_is_required_unless_cif()
    {
        // Con DNI: obligatorio.
        var withDni = _service.ValidateClient(ValidClient(c => c.LastName1 = ""));
        Assert.Contains(withDni, e => e.Field == nameof(Client.LastName1));

        // Con CIF (empresa): permitido vacío.
        var withCif = _service.ValidateClient(ValidClient(c =>
        {
            c.TaxId = "A46103834";
            c.FirstName = "Empresa SL";
            c.LastName1 = "";
            c.LastName2 = "";
        }));
        Assert.DoesNotContain(withCif, e => e.Field == nameof(Client.LastName1));
        Assert.Empty(withCif);
    }

    [Fact]
    public void Rule_7_second_surname_is_required_unless_cif_or_nie()
    {
        // Con DNI: obligatorio.
        var withDni = _service.ValidateClient(ValidClient(c => c.LastName2 = ""));
        Assert.Contains(withDni, e => e.Field == nameof(Client.LastName2));

        // Con NIE (extranjero): permitido vacío.
        var withNie = _service.ValidateClient(ValidClient(c => { c.TaxId = "X1234567L"; c.LastName2 = ""; }));
        Assert.DoesNotContain(withNie, e => e.Field == nameof(Client.LastName2));
        Assert.Empty(withNie);

        // Con CIF (empresa): permitido vacío.
        var withCif = _service.ValidateClient(ValidClient(c =>
        {
            c.TaxId = "A46103834";
            c.FirstName = "Empresa SL";
            c.LastName1 = "";
            c.LastName2 = "";
        }));
        Assert.DoesNotContain(withCif, e => e.Field == nameof(Client.LastName2));
    }

    [Theory]
    [InlineData("912345678", false)]       // fijo: empieza por 9
    [InlineData("61234567", false)]        // 8 dígitos
    [InlineData("", false)]                // obligatorio
    [InlineData("+34612345678", true)]     // con prefijo
    [InlineData("612 34 56 78", true)]     // con espacios (se normalizan)
    public void Rule_2_phone_must_be_a_spanish_mobile(string phone, bool isValid)
    {
        var errors = _service.ValidateClient(ValidClient(c => c.Phone = phone));

        Assert.Equal(isValid, errors.All(e => e.Field != nameof(Client.Phone)));
    }

    [Theory]
    [InlineData("", false)]                // obligatorio
    [InlineData("juan.example.com", false)]
    [InlineData("juan@ejemplo", false)]    // sin punto en el dominio
    [InlineData("juan@example.com", true)]
    public void Rule_3_email_is_required_and_well_formed(string email, bool isValid)
    {
        var errors = _service.ValidateClient(ValidClient(c => c.Email = email));

        Assert.Equal(isValid, errors.All(e => e.Field != nameof(Client.Email)));
    }

    [Fact]
    public void Rule_4_postal_code_must_have_five_digits()
    {
        var errors = _service.ValidateClient(ValidClient(c => c.PostalCode = "1234"));
        Assert.Contains(errors, e => e.Field == nameof(Client.PostalCode) && e.Message.Contains("5 dígitos"));
    }

    [Fact]
    public void Rule_4_postal_code_must_exist()
    {
        var errors = _service.ValidateClient(ValidClient(c => c.PostalCode = "88888"));
        Assert.Contains(errors, e => e.Field == nameof(Client.PostalCode) && e.Message.Contains("no existe"));
    }

    [Fact]
    public void Rule_4_postal_code_must_match_the_city()
    {
        // Alegría-Dulantzi está en Araba/Álava, pero el CP 28001 es de Madrid.
        var errors = _service.ValidateClient(ValidClient(c =>
        {
            c.PostalCode = "28001";
            c.City = "Alegría-Dulantzi";
            c.Province = "Araba/Álava";
        }));
        Assert.Contains(errors, e =>
            e.Field == nameof(Client.PostalCode) && e.Message.Contains("no corresponde a la ciudad"));
    }

    [Fact]
    public void Rule_5_city_must_belong_to_the_province()
    {
        var errors = _service.ValidateClient(ValidClient(c => c.Province = "Barcelona"));
        Assert.Contains(errors, e =>
            e.Field == nameof(Client.City) && e.Message.Contains("no pertenece a la provincia"));
    }

    [Fact]
    public void Rule_8_address_and_registration_date_are_required()
    {
        var errors = _service.ValidateClient(ValidClient(c =>
        {
            c.Address = "";
            c.RegistrationDate = default;
        }));
        Assert.Contains(errors, e => e.Field == nameof(Client.Address));
        Assert.Contains(errors, e => e.Field == nameof(Client.RegistrationDate));
    }

    [Fact]
    public void Rule_8_notes_are_the_only_optional_field()
    {
        Assert.Empty(_service.ValidateClient(ValidClient(c => c.Notes = "")));
    }

    [Fact]
    public void Rule_9_document_must_be_unique_in_the_table()
    {
        Client first = ValidClient();
        Client second = ValidClient(c => c.FirstName = "Pedro");

        var errors = _service.ValidateClients(new[] { first, second });

        Assert.Equal(2, errors.Count(e => e.Error.Field == nameof(Client.TaxId)
            && e.Error.Message.Contains("repetido")));
        Assert.Equal(0, errors[0].RowIndex);
        Assert.Equal(1, errors[1].RowIndex);
    }

    [Fact]
    public void Phone_is_normalized_by_removing_spaces_and_hyphens()
    {
        Assert.Equal("+34612345678", ClientService.NormalizePhone(" +34 612-345-678 "));
    }
}
