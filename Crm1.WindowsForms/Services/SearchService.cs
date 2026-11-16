using System.Globalization;
using System.Text;
using Crm1.WindowsForms.Models;
using Task = Crm1.WindowsForms.Models.Task;
using TaskStatus = Crm1.WindowsForms.Models.TaskStatus;

namespace Crm1.WindowsForms.Services;

/// <summary>
/// Módulo al que pertenece un resultado de la búsqueda global.
/// </summary>
public enum SearchModule
{
    /// <summary>Cliente registrado (tabla de ClientForm).</summary>
    Client,

    /// <summary>Futuro cliente (tabla de ContactForm).</summary>
    Contact,

    /// <summary>Tarea (tabla de TaskForm, specs/005).</summary>
    Task,

    /// <summary>Recordatorio (tabla de ReminderForm, specs/007).</summary>
    Reminder
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

    /// <summary>Registro completo encontrado cuando es una tarea.</summary>
    public Task? Task { get; init; }

    /// <summary>Registro completo encontrado cuando es un recordatorio.</summary>
    public Reminder? Reminder { get; init; }

    /// <summary>
    /// Registro vinculado con el prefijo del módulo («(Cliente) Ana»), para
    /// tareas y recordatorios; «—» si el vínculo está roto.
    /// </summary>
    public string RelatedName { get; init; } = string.Empty;
}

/// <summary>
/// Búsqueda global sobre clientes y futuros clientes: coincidencia total o
/// parcial, sin mayúsculas ni acentos, en los campos visibles de las tablas
/// (specs/004). Lógica fuera de los formularios (constitución, punto 3).
/// </summary>
public static class SearchService
{
    /// <summary>
    /// Devuelve las coincidencias: primero clientes, luego futuros clientes,
    /// después tareas y por último recordatorios, en el orden de las listas.
    /// Un registro ocupa una fila con todos sus campos coincidentes agrupados;
    /// un término vacío no devuelve nada.
    /// </summary>
    public static IReadOnlyList<SearchMatch> Search(IEnumerable<Client> clients, IEnumerable<Contact> contacts, string? term)
        => Search(clients, contacts, Array.Empty<Task>(), Array.Empty<Reminder>(), term);

    /// <summary>
    /// Búsqueda global completa (specs/004): clientes, futuros clientes,
    /// tareas (specs/005) y recordatorios (specs/007), con las mismas reglas
    /// de coincidencia para todos.
    /// </summary>
    public static IReadOnlyList<SearchMatch> Search(
        IEnumerable<Client> clients,
        IEnumerable<Contact> contacts,
        IEnumerable<Task> tasks,
        IEnumerable<Reminder> reminders,
        string? term)
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
                client, null, null, null,
                BuildDisplayName(client.FirstName, client.LastName1, client.LastName2),
                ClientFields(client), normalizedTerm);
        }

        foreach (Contact contact in contacts)
        {
            AddMatch(matches, SearchModule.Contact, contact.Id,
                null, contact, null, null,
                BuildDisplayName(contact.FirstName, contact.LastName1, contact.LastName2),
                ContactFields(contact), normalizedTerm);
        }

        // Nombres de los registros vinculados, resueltos una sola vez.
        Dictionary<Guid, string> relatedNames = BuildRelatedNames(clients, contacts);

        foreach (Task task in tasks)
        {
            AddMatch(matches, SearchModule.Task, task.Id,
                null, null, task, null,
                task.Action,
                TaskFields(task, relatedNames.GetValueOrDefault(task.RelatedId, "—")),
                normalizedTerm);
        }

        foreach (Reminder reminder in reminders)
        {
            AddMatch(matches, SearchModule.Reminder, reminder.Id,
                null, null, null, reminder,
                reminder.Subject,
                ReminderFields(reminder, relatedNames.GetValueOrDefault(reminder.RelatedId, "—")),
                normalizedTerm);
        }

        return matches;
    }

    /// <summary>
    /// Diccionario de registro vinculado → nombre con prefijo del módulo
    /// («(Cliente) Ana», «(Futuro cliente) Iker»), igual que en las tablas de
    /// tareas y recordatorios; lo usan la búsqueda y los formularios.
    /// </summary>
    public static Dictionary<Guid, string> BuildRelatedNames(IEnumerable<Client> clients, IEnumerable<Contact> contacts)
    {
        var names = new Dictionary<Guid, string>();
        foreach (Client client in clients)
        {
            names[client.Id] = $"(Cliente) {BuildDisplayName(client.FirstName, client.LastName1, client.LastName2)}";
        }
        foreach (Contact contact in contacts)
        {
            names[contact.Id] = $"(Futuro cliente) {BuildDisplayName(contact.FirstName, contact.LastName1, contact.LastName2)}";
        }
        return names;
    }

    // Añade una coincidencia si algún campo del registro contiene el término.
    private static void AddMatch(
        List<SearchMatch> matches,
        SearchModule module,
        Guid id,
        Client? client,
        Contact? contact,
        Task? task,
        Reminder? reminder,
        string displayName,
        (string Label, string Value)[] fields,
        string normalizedTerm)
    {
        var matched = new List<string>();
        foreach ((string label, string value) in fields)
        {
            // El «—» de un vínculo roto se muestra, pero no se busca.
            if (value.Length == 0 || value == "—"
                || !NormalizeText(value).Contains(normalizedTerm, StringComparison.Ordinal))
            {
                continue;
            }
            matched.Add($"{label}: {value}");
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
                Contact = contact,
                Task = task,
                Reminder = reminder,
                RelatedName = module is SearchModule.Task or SearchModule.Reminder
                    ? fields.FirstOrDefault(field => field.Label == "Cliente/Futuro cliente").Value
                    : string.Empty
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

    // Campos visibles de una tarea (specs/005), con las etiquetas en español
    // de la tabla del módulo; el vínculo se busca por su nombre resuelto.
    private static (string Label, string Value)[] TaskFields(Task task, string relatedName) => new[]
    {
        ("Título", task.Action),
        ("Descripción", task.Description),
        ("Cliente/Futuro cliente", relatedName),
        ("Fecha límite", task.DueDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)),
        ("Estado", TaskStatusLabel(task.Status)),
        ("Prioridad", task.Priority switch
        {
            TaskPriority.High => "Alta",
            TaskPriority.Low => "Baja",
            _ => "Media"
        })
    };

    // Campos visibles de un recordatorio (specs/007), con sus etiquetas.
    private static (string Label, string Value)[] ReminderFields(Reminder reminder, string relatedName) => new[]
    {
        ("Asunto", reminder.Subject),
        ("Cliente/Futuro cliente", relatedName),
        ("Fecha y hora", reminder.RemindAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)),
        ("Estado", reminder.Status == ReminderStatus.Dismissed ? "Descartado" : "Activo")
    };

    // Etiqueta en español del estado de una tarea (igual que TaskForm).
    private static string TaskStatusLabel(TaskStatus status) => status switch
    {
        TaskStatus.Completed => "Completada",
        TaskStatus.InProgress => "En curso",
        _ => "Pendiente"
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
