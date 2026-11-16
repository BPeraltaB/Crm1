using Crm1.WindowsForms.Models;
using Crm1.WindowsForms.Services;
using Xunit;
using Task = Crm1.WindowsForms.Models.Task;

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

    // Pruebas de la ampliación a tareas y recordatorios (specs/004 §7).

    [Fact]
    public void Search_TaskMatchesByTitleWithResolvedRelatedName()
    {
        Client ana = NewClient("Ana");
        Task task = NewTask("Llamar para la propuesta", ana.Id, TaskRelatedType.Client);

        IReadOnlyList<SearchMatch> matches = SearchService.Search(
            new List<Client> { ana }, new List<Contact>(), new List<Task> { task }, new List<Reminder>(), "llamar");

        SearchMatch match = Assert.Single(matches);
        Assert.Equal(SearchModule.Task, match.Module);
        Assert.Equal(task.Id, match.Id);
        Assert.Equal("Llamar para la propuesta", match.DisplayName);
        Assert.Same(task, match.Task);
        Assert.Equal("(Cliente) Ana", match.RelatedName);
        Assert.Contains("Título: Llamar para la propuesta", match.MatchedText);
    }

    [Fact]
    public void Search_TaskMatchesByDescriptionStatusAndPriorityLabels()
    {
        Client ana = NewClient("Ana");
        Task task = NewTask("Otra cosa", ana.Id, TaskRelatedType.Client,
            status: Crm1.WindowsForms.Models.TaskStatus.Completed, priority: TaskPriority.High);

        Assert.Single(SearchService.Search(
            new List<Client>(), new List<Contact>(), new List<Task> { task }, new List<Reminder>(), "completada"));
        Assert.Single(SearchService.Search(
            new List<Client>(), new List<Contact>(), new List<Task> { task }, new List<Reminder>(), "alta"));
        Assert.Single(SearchService.Search(
            new List<Client>(), new List<Contact>(), new List<Task> { task }, new List<Reminder>(), "propuesta"));
    }

    [Fact]
    public void Search_ReminderMatchesBySubjectAndDateTimeWithSeconds()
    {
        Contact eva = NewContact("Eva");
        Reminder reminder = new()
        {
            Id = Guid.NewGuid(),
            Subject = "Llamada de seguimiento",
            RemindAt = new DateTime(2026, 11, 20, 10, 30, 0),
            Status = ReminderStatus.Active,
            RelatedId = eva.Id,
            RelatedType = TaskRelatedType.Prospect
        };

        SearchMatch match = Assert.Single(SearchService.Search(
            new List<Client>(), new List<Contact> { eva }, new List<Task>(), new List<Reminder> { reminder }, "seguimiento"));
        Assert.Equal(SearchModule.Reminder, match.Module);
        Assert.Equal("(Futuro cliente) Eva", match.RelatedName);

        Assert.Single(SearchService.Search(
            new List<Client>(), new List<Contact> { eva }, new List<Task>(), new List<Reminder> { reminder }, "20/11/2026 10:30"));
    }

    [Fact]
    public void Search_BrokenLinkIsNotSearchableButShowsAsDash()
    {
        Task orphan = NewTask("Tarea huérfana", Guid.NewGuid(), TaskRelatedType.Client);

        SearchMatch match = Assert.Single(SearchService.Search(
            new List<Client>(), new List<Contact>(), new List<Task> { orphan }, new List<Reminder>(), "huérfana"));

        Assert.Equal("—", match.RelatedName);
        Assert.Empty(SearchService.Search(
            new List<Client>(), new List<Contact>(), new List<Task> { orphan }, new List<Reminder>(), "—"));
    }

    [Fact]
    public void Search_ReturnsModulesInOrderClientContactTaskReminder()
    {
        Client ana = NewClient("Ana");
        Contact contact = NewContact("Ana");
        Task task = NewTask("Hablar con Ana", ana.Id, TaskRelatedType.Client);
        Reminder reminder = new()
        {
            Id = Guid.NewGuid(),
            Subject = "Llamar a Ana",
            RemindAt = new DateTime(2026, 11, 20, 10, 30, 0),
            RelatedId = contact.Id,
            RelatedType = TaskRelatedType.Prospect
        };

        IReadOnlyList<SearchMatch> matches = SearchService.Search(
            new List<Client> { ana }, new List<Contact> { contact },
            new List<Task> { task }, new List<Reminder> { reminder }, "ana");

        Assert.Equal(4, matches.Count);
        Assert.Equal(SearchModule.Client, matches[0].Module);
        Assert.Equal(SearchModule.Contact, matches[1].Module);
        Assert.Equal(SearchModule.Task, matches[2].Module);
        Assert.Equal(SearchModule.Reminder, matches[3].Module);
    }

    // Tarea de prueba vinculada al registro indicado.
    private static Task NewTask(string action, Guid relatedId, TaskRelatedType relatedType,
        Crm1.WindowsForms.Models.TaskStatus status = Crm1.WindowsForms.Models.TaskStatus.Pending,
        TaskPriority priority = TaskPriority.Medium) => new()
    {
        Id = Guid.NewGuid(),
        Action = action,
        Description = "Presentar la propuesta",
        DueDate = new DateTime(2026, 11, 20),
        Status = status,
        Priority = priority,
        RelatedId = relatedId,
        RelatedType = relatedType
    };

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
