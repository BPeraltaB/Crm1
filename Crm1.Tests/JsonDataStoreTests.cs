using System.Text;
using Crm1.WindowsForms.Models;
using Crm1.WindowsForms.Services;
using Xunit;

namespace Crm1.Tests;

/// <summary>
/// Pruebas del almacenamiento JSON de clientes.
/// </summary>
public sealed class JsonDataStoreTests : IDisposable
{
    private readonly string _directory;

    public JsonDataStoreTests()
    {
        _directory = Path.Combine(
            Path.GetTempPath(), "opencode", "crm1-tests", $"store-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    private string StorePath => Path.Combine(_directory, "Data", "clients.json");

    private string ContactsStorePath => Path.Combine(_directory, "Data", "contacts.json");

    private static Client SampleClient() => new()
    {
        Id = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"),
        FirstName = "José",
        LastName1 = "Álvarez",
        LastName2 = "Muñoz",
        TaxId = "12345678Z",
        Address = "Calle Mayor 1",
        PostalCode = "01001",
        City = "Vitoria-Gasteiz",
        Province = "Araba/Álava",
        Phone = "612345678",
        Email = "jose@example.com",
        RegistrationDate = new DateTime(2026, 2, 10),
        Notes = "Cliente de prueba",
        Status = ClientStatus.Inactive
    };

    // Futuro cliente mínimo: solo nombre y teléfono (specs/003).
    private static Contact SampleContact() => new()
    {
        Id = Guid.Parse("fedcba98-7654-3210-fedc-ba9876543210"),
        FirstName = "Lucía",
        Phone = "912345678",
        Status = ContactStatus.Pending
    };

    [Fact]
    public void Saved_clients_are_reloaded_with_all_fields()
    {
        var store = new JsonDataStore(StorePath);
        store.SaveClients(new List<Client> { SampleClient() });

        List<Client> loaded = new JsonDataStore(StorePath).LoadClients();

        Client client = Assert.Single(loaded);
        Assert.Equal(SampleClient().Id, client.Id);
        Assert.Equal("José", client.FirstName);
        Assert.Equal("Álvarez", client.LastName1);
        Assert.Equal("Muñoz", client.LastName2);
        Assert.Equal("12345678Z", client.TaxId);
        Assert.Equal("01001", client.PostalCode);
        Assert.Equal("Araba/Álava", client.Province);
        Assert.Equal(ClientStatus.Inactive, client.Status);
        Assert.Equal(new DateTime(2026, 2, 10), client.RegistrationDate);
        Assert.Equal("Cliente de prueba", client.Notes);
    }

    [Fact]
    public void Missing_file_returns_an_empty_list()
    {
        Assert.Empty(new JsonDataStore(StorePath).LoadClients());
    }

    [Fact]
    public void Corrupted_file_throws_a_visible_error()
    {
        string path = Path.Combine(_directory, "clients.json");
        File.WriteAllText(path, "{ esto no es json válido", Encoding.UTF8);

        Assert.Throws<InvalidOperationException>(() => new JsonDataStore(path).LoadClients());
    }

    [Fact]
    public void Leaving_without_saving_does_not_modify_the_file()
    {
        var store = new JsonDataStore(StorePath);
        store.SaveClients(new List<Client> { SampleClient() });
        string fileContent = File.ReadAllText(StorePath, Encoding.UTF8);

        // Se cargan los clientes y se modifican en memoria sin volver a guardar,
        // como cuando el usuario sale del formulario sin pulsar Guardar.
        List<Client> loaded = store.LoadClients();
        loaded.Add(ValidClient());

        Assert.Equal(fileContent, File.ReadAllText(StorePath, Encoding.UTF8));
    }

    [Fact]
    public void Save_creates_the_data_folder_and_leaves_no_temporary_file()
    {
        var store = new JsonDataStore(StorePath);
        store.SaveClients(new List<Client> { SampleClient() });

        Assert.True(File.Exists(StorePath));
        Assert.False(File.Exists(StorePath + ".tmp"));
    }

    [Fact]
    public void Saved_contacts_are_reloaded_with_all_fields()
    {
        var store = new JsonDataStore(StorePath, ContactsStorePath);
        store.SaveContacts(new List<Contact> { SampleContact() });

        List<Contact> loaded = new JsonDataStore(StorePath, ContactsStorePath).LoadContacts();

        Contact contact = Assert.Single(loaded);
        Assert.Equal(SampleContact().Id, contact.Id);
        Assert.Equal("Lucía", contact.FirstName);
        Assert.Equal(string.Empty, contact.LastName1);
        Assert.Equal(string.Empty, contact.TaxId);
        Assert.Equal("912345678", contact.Phone);
        Assert.Equal(string.Empty, contact.Email);
        Assert.Equal(default, contact.RegistrationDate);
        Assert.Equal(ContactStatus.Pending, contact.Status);
    }

    [Fact]
    public void Missing_contacts_file_returns_an_empty_list()
    {
        Assert.Empty(new JsonDataStore(StorePath, ContactsStorePath).LoadContacts());
    }

    [Fact]
    public void Corrupted_contacts_file_throws_a_visible_error()
    {
        string path = Path.Combine(_directory, "contacts.json");
        File.WriteAllText(path, "{ esto no es json válido", Encoding.UTF8);

        Assert.Throws<InvalidOperationException>(() => new JsonDataStore(StorePath, path).LoadContacts());
    }

    [Fact]
    public void Contacts_file_is_independent_of_the_clients_file()
    {
        var store = new JsonDataStore(StorePath, ContactsStorePath);
        store.SaveClients(new List<Client> { SampleClient() });
        string clientsContent = File.ReadAllText(StorePath, Encoding.UTF8);

        // Guardar futuros clientes no toca el archivo de clientes.
        store.SaveContacts(new List<Contact> { SampleContact() });

        Assert.Equal(clientsContent, File.ReadAllText(StorePath, Encoding.UTF8));
        Assert.Single(store.LoadClients());
        Assert.Single(store.LoadContacts());
    }

    [Fact]
    public void Save_contacts_creates_the_data_folder_and_leaves_no_temporary_file()
    {
        var store = new JsonDataStore(StorePath, ContactsStorePath);
        store.SaveContacts(new List<Contact> { SampleContact() });

        Assert.True(File.Exists(ContactsStorePath));
        Assert.False(File.Exists(ContactsStorePath + ".tmp"));
    }

    private static Client ValidClient() => new()
    {
        FirstName = "Ana",
        LastName1 = "López",
        TaxId = "X1234567L",
        Address = "Calle Mayor 1",
        PostalCode = "28001",
        City = "Madrid",
        Province = "Madrid",
        Phone = "612345678",
        Email = "ana@example.com",
        RegistrationDate = new DateTime(2026, 3, 1)
    };
}
