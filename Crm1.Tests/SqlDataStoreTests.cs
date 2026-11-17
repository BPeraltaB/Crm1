using System.Text;
using Crm1.WindowsForms.Models;
using Crm1.WindowsForms.Services;
using Xunit;
using Task = Crm1.WindowsForms.Models.Task;
using TaskStatus = Crm1.WindowsForms.Models.TaskStatus;

namespace Crm1.Tests;

/// <summary>
/// Pruebas del almacenamiento SQL (SQLite) de specs/006: esquema automático,
///guardado/recarga transaccional, independencia de tablas, errores visibles
/// y migración única desde los JSON legados.
/// </summary>
public sealed class SqlDataStoreTests : IDisposable
{
    private readonly string _directory;

    public SqlDataStoreTests()
    {
        _directory = Path.Combine(
            Path.GetTempPath(), "opencode", "crm1-tests", $"sql-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    private string DatabasePath => Path.Combine(_directory, "crm1.db");

    // Almacén JSON legado apuntando a archivos de la carpeta temporal.
    private JsonDataStore LegacyStore => new(
        Path.Combine(_directory, "clients.json"),
        Path.Combine(_directory, "contacts.json"));

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

    private static Contact SampleContact() => new()
    {
        Id = Guid.Parse("fedcba98-7654-3210-fedc-ba9876543210"),
        FirstName = "Ana",
        LastName1 = "López",
        LastName2 = string.Empty,
        TaxId = string.Empty,
        Address = string.Empty,
        PostalCode = string.Empty,
        City = string.Empty,
        Province = string.Empty,
        Phone = "612345678",
        Email = string.Empty,
        RegistrationDate = default,          // futuro cliente sin fecha
        Notes = "Sin fecha",
        Status = ContactStatus.Discarded
    };

    [Fact]
    public void New_database_creates_tables_and_starts_empty()
    {
        SqlDataStore store = new(DatabasePath, LegacyStore);

        Assert.True(File.Exists(DatabasePath));
        Assert.Empty(store.LoadClients());
        Assert.Empty(store.LoadContacts());
        Assert.Empty(store.LoadTasks());
        Assert.Empty(store.LoadReminders());
    }

    [Fact]
    public void Clients_roundtrip_preserves_all_fields()
    {
        SqlDataStore store = new(DatabasePath, LegacyStore);
        store.SaveClients(new List<Client> { SampleClient() });

        List<Client> loaded = new SqlDataStore(DatabasePath, LegacyStore).LoadClients();

        Client client = Assert.Single(loaded);
        Assert.Equal(SampleClient().Id, client.Id);
        Assert.Equal("José", client.FirstName);
        Assert.Equal("Álvarez", client.LastName1);
        Assert.Equal("Muñoz", client.LastName2);
        Assert.Equal("12345678Z", client.TaxId);
        Assert.Equal("01001", client.PostalCode);
        Assert.Equal("Vitoria-Gasteiz", client.City);
        Assert.Equal("Araba/Álava", client.Province);
        Assert.Equal(ClientStatus.Inactive, client.Status);
        Assert.Equal(new DateTime(2026, 2, 10), client.RegistrationDate);
        Assert.Equal("Cliente de prueba", client.Notes);
    }

    [Fact]
    public void Contacts_roundtrip_preserves_empty_date_and_status()
    {
        SqlDataStore store = new(DatabasePath, LegacyStore);
        store.SaveContacts(new List<Contact> { SampleContact() });

        List<Contact> loaded = new SqlDataStore(DatabasePath, LegacyStore).LoadContacts();

        Contact contact = Assert.Single(loaded);
        Assert.Equal(SampleContact().Id, contact.Id);
        Assert.Equal("Ana", contact.FirstName);
        Assert.Equal(string.Empty, contact.LastName2);
        Assert.Equal(default, contact.RegistrationDate);
        Assert.Equal(ContactStatus.Discarded, contact.Status);
        Assert.Equal("Sin fecha", contact.Notes);
    }

    [Fact]
    public void Saving_clients_does_not_touch_contacts()
    {
        SqlDataStore store = new(DatabasePath, LegacyStore);
        store.SaveClients(new List<Client> { SampleClient() });
        store.SaveContacts(new List<Contact> { SampleContact() });

        // Guardar de nuevo los clientes no altera los futuros clientes.
        store.SaveClients(new List<Client>());

        Assert.Empty(store.LoadClients());
        Assert.Single(new SqlDataStore(DatabasePath, LegacyStore).LoadContacts());
    }

    private static Task SampleTask(TaskRelatedType relatedType = TaskRelatedType.Client) => new()
    {
        Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
        Action = "Llamar",
        Description = "Presentar la propuesta de mobiliario",
        DueDate = new DateTime(2026, 11, 20),
        Status = TaskStatus.InProgress,
        Priority = TaskPriority.High,
        RelatedId = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"),
        RelatedType = relatedType
    };

    [Fact]
    public void Tasks_roundtrip_preserves_all_fields()
    {
        SqlDataStore store = new(DatabasePath, LegacyStore);
        Task prospectTask = SampleTask(TaskRelatedType.Prospect);
        prospectTask.Id = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");
        store.SaveTasks(new List<Task> { SampleTask(), prospectTask });

        List<Task> loaded = new SqlDataStore(DatabasePath, LegacyStore).LoadTasks();

        Assert.Equal(2, loaded.Count);
        Task task = loaded[0];
        Assert.Equal(SampleTask().Id, task.Id);
        Assert.Equal("Llamar", task.Action);
        Assert.Equal("Presentar la propuesta de mobiliario", task.Description);
        Assert.Equal(new DateTime(2026, 11, 20), task.DueDate);
        Assert.Equal(TaskStatus.InProgress, task.Status);
        Assert.Equal(TaskPriority.High, task.Priority);
        Assert.Equal(SampleTask().RelatedId, task.RelatedId);
        Assert.Equal(TaskRelatedType.Client, task.RelatedType);
        Assert.Equal(TaskRelatedType.Prospect, loaded[1].RelatedType);
    }

    [Fact]
    public void Saving_tasks_does_not_touch_clients_or_contacts()
    {
        SqlDataStore store = new(DatabasePath, LegacyStore);
        store.SaveClients(new List<Client> { SampleClient() });
        store.SaveContacts(new List<Contact> { SampleContact() });
        store.SaveTasks(new List<Task> { SampleTask() });

        // Guardar de nuevo las tareas (vacías) no altera clientes ni futuros clientes.
        store.SaveTasks(new List<Task>());

        Assert.Empty(store.LoadTasks());
        Assert.Single(new SqlDataStore(DatabasePath, LegacyStore).LoadClients());
        Assert.Single(new SqlDataStore(DatabasePath, LegacyStore).LoadContacts());
    }

    private static Reminder SampleReminder(TaskRelatedType relatedType = TaskRelatedType.Client,
        ReminderStatus status = ReminderStatus.Active) => new()
    {
        Id = Guid.Parse("99999999-8888-7777-6666-555555555555"),
        Subject = "Llamada de seguimiento del presupuesto",
        RemindAt = new DateTime(2026, 11, 20, 10, 30, 0),
        Status = status,
        RelatedId = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"),
        RelatedType = relatedType
    };

    [Fact]
    public void Reminders_roundtrip_preserves_all_fields()
    {
        SqlDataStore store = new(DatabasePath, LegacyStore);
        Reminder prospectReminder = SampleReminder(TaskRelatedType.Prospect, ReminderStatus.Dismissed);
        prospectReminder.Id = Guid.Parse("aaaaaaa1-bbbb-cccc-dddd-eeeeeeeeeeee");
        store.SaveReminders(new List<Reminder> { SampleReminder(), prospectReminder });

        List<Reminder> loaded = new SqlDataStore(DatabasePath, LegacyStore).LoadReminders();

        Assert.Equal(2, loaded.Count);
        Reminder reminder = loaded[0];
        Assert.Equal(SampleReminder().Id, reminder.Id);
        Assert.Equal("Llamada de seguimiento del presupuesto", reminder.Subject);
        Assert.Equal(new DateTime(2026, 11, 20, 10, 30, 0), reminder.RemindAt);
        Assert.Equal(ReminderStatus.Active, reminder.Status);
        Assert.Equal(SampleReminder().RelatedId, reminder.RelatedId);
        Assert.Equal(TaskRelatedType.Client, reminder.RelatedType);
        Assert.Equal(TaskRelatedType.Prospect, loaded[1].RelatedType);
        Assert.Equal(ReminderStatus.Dismissed, loaded[1].Status);
    }

    [Fact]
    public void Saving_reminders_does_not_touch_other_tables()
    {
        SqlDataStore store = new(DatabasePath, LegacyStore);
        store.SaveClients(new List<Client> { SampleClient() });
        store.SaveContacts(new List<Contact> { SampleContact() });
        store.SaveTasks(new List<Task> { SampleTask() });
        store.SaveReminders(new List<Reminder> { SampleReminder() });

        // Guardar de nuevo los recordatorios (vacíos) no altera las demás tablas.
        store.SaveReminders(new List<Reminder>());

        Assert.Empty(store.LoadReminders());
        Assert.Single(new SqlDataStore(DatabasePath, LegacyStore).LoadClients());
        Assert.Single(new SqlDataStore(DatabasePath, LegacyStore).LoadContacts());
        Assert.Single(new SqlDataStore(DatabasePath, LegacyStore).LoadTasks());
    }

    [Fact]
    public void Corrupt_database_throws_a_visible_error()
    {
        File.WriteAllText(DatabasePath, "esto no es una base de datos", Encoding.UTF8);

        Assert.Throws<InvalidOperationException>(() => new SqlDataStore(DatabasePath, LegacyStore));
    }

    [Fact]
    public void Legacy_json_data_is_migrated_once()
    {
        LegacyStore.SaveClients(new List<Client> { SampleClient() });
        LegacyStore.SaveContacts(new List<Contact> { SampleContact() });

        SqlDataStore first = new(DatabasePath, LegacyStore);
        Assert.Single(first.LoadClients());
        Assert.Single(first.LoadContacts());

        // La segunda apertura no duplica la migración (idempotente).
        SqlDataStore second = new(DatabasePath, LegacyStore);
        Assert.Single(second.LoadClients());
        Assert.Single(second.LoadContacts());

        // Los JSON legados siguen en disco, intactos (constitución, punto 5).
        Assert.True(File.Exists(LegacyStore.FilePath));
        Assert.True(File.Exists(LegacyStore.ContactsFilePath));
    }

    [Fact]
    public void Migration_mark_prevents_reimport_if_json_reappears()
    {
        // Primer arranque limpio: marca asentada sin JSON.
        SqlDataStore store = new(DatabasePath, LegacyStore);
        store.SaveClients(new List<Client>());

        // Si los JSON aparecen después, no se vuelven a importar.
        LegacyStore.SaveClients(new List<Client> { SampleClient() });
        SqlDataStore reopened = new(DatabasePath, LegacyStore);

        Assert.Empty(reopened.LoadClients());
    }

    // Pruebas de la reasignación de vínculos al convertir un futuro cliente
    // en cliente (specs/003 §7, specs/005 y specs/007).

    // Copia de una tarea con otro vínculo (los modelos son clases).
    private static Task WithLink(Task task, Guid relatedId, TaskRelatedType relatedType) => new()
    {
        Id = task.Id,
        Action = task.Action,
        Description = task.Description,
        DueDate = task.DueDate,
        Status = task.Status,
        Priority = task.Priority,
        RelatedId = relatedId,
        RelatedType = relatedType
    };

    // Copia de un recordatorio con otro vínculo.
    private static Reminder WithLink(Reminder reminder, Guid relatedId, TaskRelatedType relatedType) => new()
    {
        Id = reminder.Id,
        Subject = reminder.Subject,
        RemindAt = reminder.RemindAt,
        Status = reminder.Status,
        RelatedId = relatedId,
        RelatedType = relatedType
    };

    [Fact]
    public void Relink_moves_tasks_and_reminders_of_the_converted_prospect()
    {
        SqlDataStore store = new(DatabasePath, LegacyStore);
        Guid prospectId = Guid.NewGuid();
        Guid clientId = Guid.NewGuid();

        Task prospectTask = WithLink(SampleTask(), prospectId, TaskRelatedType.Prospect);
        prospectTask.Id = Guid.NewGuid();
        Task clientTask = SampleTask(); // Ya vinculada a un cliente: no se toca.
        Reminder prospectReminder = WithLink(SampleReminder(), prospectId, TaskRelatedType.Prospect);
        store.SaveTasks(new List<Task> { prospectTask, clientTask });
        store.SaveReminders(new List<Reminder> { prospectReminder });

        int moved = store.RelinkRelatedRecords(prospectId, clientId);

        Assert.Equal(2, moved);
        List<Task> tasks = store.LoadTasks();
        Task movedTask = tasks.Single(task => task.Id == prospectTask.Id);
        Assert.Equal(clientId, movedTask.RelatedId);
        Assert.Equal(TaskRelatedType.Client, movedTask.RelatedType);
        Task untouchedTask = tasks.Single(task => task.Id == clientTask.Id);
        Assert.Equal(SampleTask().RelatedId, untouchedTask.RelatedId);
        Assert.Equal(TaskRelatedType.Client, untouchedTask.RelatedType);

        Reminder movedReminder = store.LoadReminders().Single();
        Assert.Equal(clientId, movedReminder.RelatedId);
        Assert.Equal(TaskRelatedType.Client, movedReminder.RelatedType);
    }

    [Fact]
    public void Relink_without_matching_rows_returns_zero_and_changes_nothing()
    {
        SqlDataStore store = new(DatabasePath, LegacyStore);
        Guid prospectId = Guid.NewGuid();
        store.SaveTasks(new List<Task> { SampleTask() });
        store.SaveReminders(new List<Reminder> { SampleReminder() });

        int moved = store.RelinkRelatedRecords(prospectId, Guid.NewGuid());

        Assert.Equal(0, moved);
        Assert.Equal(SampleTask().RelatedId, store.LoadTasks().Single().RelatedId);
        Assert.Equal(SampleReminder().RelatedId, store.LoadReminders().Single().RelatedId);
    }

    [Fact]
    public void Relink_does_not_touch_links_of_other_prospects()
    {
        // Dos futuros distintos: solo el convertido cambia de vínculo.
        SqlDataStore store = new(DatabasePath, LegacyStore);
        Guid convertedId = Guid.NewGuid();
        Guid otherProspectId = Guid.NewGuid();
        Guid clientId = Guid.NewGuid();

        Task converted = WithLink(SampleTask(), convertedId, TaskRelatedType.Prospect);
        Task other = WithLink(SampleTask(), otherProspectId, TaskRelatedType.Prospect);
        other.Id = Guid.Parse("77777777-7777-7777-7777-777777777777");
        store.SaveTasks(new List<Task> { converted, other });

        int moved = store.RelinkRelatedRecords(convertedId, clientId);

        Assert.Equal(1, moved);
        List<Task> tasks = store.LoadTasks();
        Assert.Equal(TaskRelatedType.Client, tasks.Single(task => task.Id == converted.Id).RelatedType);
        Task untouched = tasks.Single(task => task.Id == other.Id);
        Assert.Equal(TaskRelatedType.Prospect, untouched.RelatedType);
        Assert.Equal(otherProspectId, untouched.RelatedId);
    }

    // Pruebas de las tablas Quotes y Orders (specs/009 y specs/006).

    private static Quote SampleQuote(TaskRelatedType relatedType = TaskRelatedType.Client,
        QuoteStatus status = QuoteStatus.Accepted, Guid? orderId = null) => new()
    {
        Id = Guid.Parse("22222222-3333-4444-5555-666666666666"),
        QuoteDate = new DateTime(2026, 11, 10),
        Amount = 1234.56m,
        Reference = "P-2026-0142",
        Description = "Mobiliario de oficina",
        Status = status,
        RelatedId = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"),
        RelatedType = relatedType,
        OrderId = orderId
    };

    [Fact]
    public void Quotes_roundtrip_preserves_all_fields()
    {
        SqlDataStore store = new(DatabasePath, LegacyStore);
        Guid orderId = Guid.NewGuid();
        Quote quote = SampleQuote(TaskRelatedType.Prospect, QuoteStatus.Dismissed, orderId);
        store.SaveQuotes(new List<Quote> { quote });

        Quote loaded = new SqlDataStore(DatabasePath, LegacyStore).LoadQuotes().Single();

        Assert.Equal(quote.Id, loaded.Id);
        Assert.Equal(new DateTime(2026, 11, 10), loaded.QuoteDate);
        Assert.Equal(1234.56m, loaded.Amount);
        Assert.Equal("P-2026-0142", loaded.Reference);
        Assert.Equal("Mobiliario de oficina", loaded.Description);
        Assert.Equal(QuoteStatus.Dismissed, loaded.Status);
        Assert.Equal(TaskRelatedType.Prospect, loaded.RelatedType);
        Assert.Equal(orderId, loaded.OrderId);
    }

    [Fact]
    public void Quote_without_order_roundtrips_a_null_order_id()
    {
        SqlDataStore store = new(DatabasePath, LegacyStore);
        store.SaveQuotes(new List<Quote> { SampleQuote() });

        Assert.Null(store.LoadQuotes().Single().OrderId);
    }

    [Fact]
    public void Saving_quotes_does_not_touch_other_tables()
    {
        SqlDataStore store = new(DatabasePath, LegacyStore);
        store.SaveClients(new List<Client> { SampleClient() });
        store.SaveTasks(new List<Task> { SampleTask() });
        store.SaveOrders(new List<Order> { SampleOrder() });

        store.SaveQuotes(new List<Quote> { SampleQuote() });

        Assert.Single(store.LoadQuotes());
        Assert.Single(store.LoadClients());
        Assert.Single(store.LoadTasks());
        Assert.Single(store.LoadOrders());
    }

    private static Order SampleOrder(OrderKind kind = OrderKind.CreditNote) => new()
    {
        Id = Guid.Parse("33333333-4444-5555-6666-777777777777"),
        Kind = kind,
        OrderDate = new DateTime(2026, 11, 12),
        Amount = 9876.54m,
        Reference = "E-2026-0099",
        Description = "Sillas ergonómicas",
        ClientId = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef")
    };

    [Fact]
    public void Orders_roundtrip_preserves_all_fields()
    {
        SqlDataStore store = new(DatabasePath, LegacyStore);
        Order creditNote = SampleOrder(OrderKind.CreditNote);
        creditNote.Id = Guid.Parse("44444444-5555-6666-7777-888888888888");
        store.SaveOrders(new List<Order> { SampleOrder(OrderKind.Order), creditNote });

        List<Order> loaded = new SqlDataStore(DatabasePath, LegacyStore).LoadOrders();

        Assert.Equal(2, loaded.Count);
        Order order = loaded[0];
        Assert.Equal(SampleOrder().Id, order.Id);
        Assert.Equal(OrderKind.Order, order.Kind);
        Assert.Equal(new DateTime(2026, 11, 12), order.OrderDate);
        Assert.Equal(9876.54m, order.Amount);
        Assert.Equal("E-2026-0099", order.Reference);
        Assert.Equal("Sillas ergonómicas", order.Description);
        Assert.Equal(SampleOrder().ClientId, order.ClientId);
        Assert.Equal(OrderKind.CreditNote, loaded[1].Kind);
    }

    [Fact]
    public void Relink_moves_quotes_of_the_converted_prospect()
    {
        SqlDataStore store = new(DatabasePath, LegacyStore);
        Guid prospectId = Guid.NewGuid();
        Guid clientId = Guid.NewGuid();
        Quote prospectQuote = SampleQuote(TaskRelatedType.Prospect, QuoteStatus.Pending);
        prospectQuote.RelatedId = prospectId;
        Quote clientQuote = SampleQuote();
        clientQuote.Id = Guid.Parse("55555555-6666-7777-8888-999999999999");
        store.SaveQuotes(new List<Quote> { prospectQuote, clientQuote });

        int moved = store.RelinkRelatedRecords(prospectId, clientId);

        Assert.Equal(1, moved);
        List<Quote> quotes = store.LoadQuotes();
        Assert.Equal(clientId, quotes.Single(quote => quote.Id == prospectQuote.Id).RelatedId);
        Assert.Equal(TaskRelatedType.Client, quotes.Single(quote => quote.Id == prospectQuote.Id).RelatedType);
        Assert.Equal(SampleQuote().RelatedId, quotes.Single(quote => quote.Id == clientQuote.Id).RelatedId);
    }
}
