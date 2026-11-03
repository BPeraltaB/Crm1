using Crm1.WindowsForms.Models;
using Crm1.WindowsForms.Services;
using Xunit;

namespace Crm1.Tests;

/// <summary>
/// Pruebas de la búsqueda global (specs/004): coincidencia total o parcial,
/// sin mayúsculas ni acentos, sobre clientes y futuros clientes guardados.
/// </summary>
public sealed class SearchServiceTests
{
    [Fact]
    public void Search_WithEmptyTerm_ReturnsNoMatches()
    {
        IReadOnlyList<SearchMatch> matches = SearchService.Search(
            new List<Client> { NewClient("Ana") },
            new List<Contact> { NewContact("Eva") },
            "   ");

        Assert.Empty(matches);
    }

    [Fact]
    public void Search_PartialTerm_IgnoresCaseAndAccents()
    {
        Client client = NewClient("Ana", city: "Málaga");

        IReadOnlyList<SearchMatch> matches = SearchService.Search(
            new List<Client> { client }, new List<Contact>(), "MALAGA");

        SearchMatch match = Assert.Single(matches);
        Assert.Equal(client.Id, match.Id);
        Assert.Contains("Ciudad: Málaga", match.MatchedText);
    }

    [Fact]
    public void Search_ExactValue_FullMatch()
    {
        Client client = NewClient("Ana", taxId: "12345678Z");

        IReadOnlyList<SearchMatch> matches = SearchService.Search(
            new List<Client> { client }, new List<Contact>(), "12345678Z");

        Assert.Single(matches);
    }

    [Fact]
    public void Search_MatchesAnyVisibleField()
    {
        Client client = NewClient("Ana", notes: "Cliente importante del grupo norte");

        IReadOnlyList<SearchMatch> matches = SearchService.Search(
            new List<Client> { client }, new List<Contact>(), "importante");

        SearchMatch match = Assert.Single(matches);
        Assert.Contains("Observaciones: Cliente importante del grupo norte", match.MatchedText);
    }

    [Fact]
    public void Search_ContactWithoutDate_DoesNotMatchDefaultDate()
    {
        // Futuro cliente sin fecha de alta (fecha por defecto): "01/01/0001" no debe buscarlo.
        Contact contact = NewContact("Eva");

        IReadOnlyList<SearchMatch> matches = SearchService.Search(
            new List<Client>(), new List<Contact> { contact }, "01/01/0001");

        Assert.Empty(matches);
    }

    [Fact]
    public void Search_ContactStatus_MatchesInSpanish()
    {
        Contact contact = NewContact("Eva");
        contact.Status = ContactStatus.Discarded;

        IReadOnlyList<SearchMatch> matches = SearchService.Search(
            new List<Client>(), new List<Contact> { contact }, "descartado");

        SearchMatch match = Assert.Single(matches);
        Assert.Equal(SearchModule.Contact, match.Module);
        Assert.Contains("Estado: Descartado", match.MatchedText);
    }

    [Fact]
    public void Search_DateMatchesInDayMonthYearFormat()
    {
        Client client = NewClient("Ana");
        client.RegistrationDate = new DateTime(2026, 10, 27);

        IReadOnlyList<SearchMatch> matches = SearchService.Search(
            new List<Client> { client }, new List<Contact>(), "27/10/2026");

        SearchMatch match = Assert.Single(matches);
        Assert.Contains("Fecha de alta: 27/10/2026", match.MatchedText);
    }

    [Fact]
    public void Search_MultipleMatchingFields_ProduceSingleRowWithAllFields()
    {
        // "Ana" coincide con el nombre y con "Santander" por la "an"... mejor:
        // un término que cae en dos campos distintos del mismo registro.
        Client client = NewClient("Lucía", city: "Lucía");

        IReadOnlyList<SearchMatch> matches = SearchService.Search(
            new List<Client> { client }, new List<Contact>(), "lucia");

        SearchMatch match = Assert.Single(matches);
        Assert.Contains("Nombre: Lucía", match.MatchedText);
        Assert.Contains("Ciudad: Lucía", match.MatchedText);
    }

    [Fact]
    public void Search_ClientsAppearBeforeContacts()
    {
        Client client = NewClient("Ana");
        Contact contact = NewContact("Ana");

        IReadOnlyList<SearchMatch> matches = SearchService.Search(
            new List<Client> { client }, new List<Contact> { contact }, "ana");

        Assert.Equal(2, matches.Count);
        Assert.Equal(SearchModule.Client, matches[0].Module);
        Assert.Equal(SearchModule.Contact, matches[1].Module);
    }

    [Fact]
    public void Search_NoMatches_ReturnsEmptyList()
    {
        IReadOnlyList<SearchMatch> matches = SearchService.Search(
            new List<Client> { NewClient("Ana") }, new List<Contact>(), "zzzz");

        Assert.Empty(matches);
    }

    [Fact]
    public void Search_DisplayNameJoinsNameAndSurnames()
    {
        Client client = NewClient("Ana", lastName1: "García", lastName2: "Ruiz");

        IReadOnlyList<SearchMatch> matches = SearchService.Search(
            new List<Client> { client }, new List<Contact>(), "garcia");

        SearchMatch match = Assert.Single(matches);
        Assert.Equal("Ana García Ruiz", match.DisplayName);
    }

    [Fact]
    public void Search_ExposesTheFullRecord()
    {
        Client client = NewClient("Ana", city: "Madrid");

        IReadOnlyList<SearchMatch> matches = SearchService.Search(
            new List<Client> { client }, new List<Contact>(), "madrid");

        SearchMatch match = Assert.Single(matches);
        Assert.Same(client, match.Client);
        Assert.Null(match.Contact);
    }

    // Cliente de prueba con datos opcionales.
    private static Client NewClient(
        string firstName,
        string lastName1 = "",
        string lastName2 = "",
        string city = "",
        string taxId = "",
        string notes = "") => new()
    {
        Id = Guid.NewGuid(),
        FirstName = firstName,
        LastName1 = lastName1,
        LastName2 = lastName2,
        City = city,
        TaxId = taxId,
        Notes = notes,
        RegistrationDate = new DateTime(2026, 1, 15)
    };

    // Futuro cliente de prueba.
    private static Contact NewContact(string firstName) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = firstName,
        Phone = "600123456"
    };
}
