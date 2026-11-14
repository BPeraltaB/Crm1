using System.Reflection;
using Crm1.WindowsForms.Models;
using Crm1.WindowsForms.Services;
using Task = Crm1.WindowsForms.Models.Task;

namespace Crm1.Tests;

/// <summary>
/// Entorno de datos de las pruebas de interfaz: los formularios reales leen y
/// escriben en la base de datos SQL de Data/crm1.db junto al ejecutable de
/// pruebas (specs/006). Los JSON de siembra se migran solos al abrirse la base
/// por primera vez, así que aquí se preparan escenarios controlados antes de
/// crear cada formulario y se relee la base para comprobar lo guardado.
/// </summary>
internal static class UiTestEnvironment
{
    public static string DataDirectory => Path.Combine(AppContext.BaseDirectory, "Data");

    /// <summary>Base de datos SQL que usan los formularios (specs/006).</summary>
    public static string DatabasePath => Path.Combine(DataDirectory, "crm1.db");

    // JSON legados de siembra: los migrará la primera apertura de la base.
    public static string ClientsPath => Path.Combine(DataDirectory, "clients.json");

    public static string ContactsPath => Path.Combine(DataDirectory, "contacts.json");

    private static JsonDataStore SeedStore => new(ClientsPath, ContactsPath);

    // Almacén SQL directo sobre el mismo archivo que usan los formularios.
    private static SqlDataStore Store => new(DatabasePath, SeedStore);

    /// <summary>
    /// Comprueba que el catálogo de códigos postales está en la salida de
    /// pruebas: sin él los formularios mostrarían un diálogo al construirse.
    /// </summary>
    public static void EnsureCatalog()
    {
        if (!File.Exists(Path.Combine(DataDirectory, "postal-codes-es.json")))
        {
            throw new InvalidOperationException(
                "Falta Data/postal-codes-es.json en la salida de pruebas (debe copiarse al compilar).");
        }
    }

    /// <summary>
    /// Siembra el escenario de clientes: renueva la base (si ya existía de una
    /// prueba anterior) y escribe el JSON que migrará la primera apertura.
    /// También sirve para simular que otro módulo escribe clientes mientras un
    /// formulario está abierto: la base se renueva y el JSON se migra al releer.
    /// </summary>
    public static void WriteClients(params Client[] clients)
    {
        Directory.CreateDirectory(DataDirectory);
        if (File.Exists(DatabasePath))
        {
            ResetDatabase();
            File.Delete(ContactsPath);
        }
        SeedStore.SaveClients(clients.ToList());
    }

    /// <summary>
    /// Siembra el escenario de futuros clientes, igual que WriteClients. Suele
    /// llamarse justo después de WriteClients para poblar ambos módulos.
    /// </summary>
    public static void WriteContacts(params Contact[] contacts)
    {
        Directory.CreateDirectory(DataDirectory);
        if (File.Exists(DatabasePath))
        {
            ResetDatabase();
            File.Delete(ClientsPath);
        }
        SeedStore.SaveContacts(contacts.ToList());
    }

    public static List<Client> ReadClients() => Store.LoadClients();

    public static List<Contact> ReadContacts() => Store.LoadContacts();

    /// <summary>
    /// Siembra el escenario de tareas escribiendo directamente en la base de
    /// datos (las tareas no tienen JSON: specs/005 y specs/006). Reemplaza la
    /// tabla Tasks por completo; los clientes y futuros clientes sembrados
    /// antes con WriteClients/WriteContacts se migran en la primera apertura
    /// de la base que provoque este método.
    /// </summary>
    public static void WriteTasks(params Task[] tasks)
    {
        Directory.CreateDirectory(DataDirectory);
        Store.SaveTasks(tasks.ToList());
    }

    public static List<Task> ReadTasks() => Store.LoadTasks();

    /// <summary>
    /// Siembra el escenario de recordatorios escribiendo directamente en la
    /// base de datos (los recordatorios no tienen JSON: specs/007 y specs/006).
    /// Reemplaza la tabla Reminders por completo; el resto de tablas queda
    /// intacto.
    /// </summary>
    public static void WriteReminders(params Reminder[] reminders)
    {
        Directory.CreateDirectory(DataDirectory);
        Store.SaveReminders(reminders.ToList());
    }

    public static List<Reminder> ReadReminders() => Store.LoadReminders();

    /// <summary>
    /// Fuerza un sello temporal distinto en la base de datos para simular que
    /// otro módulo la ha escrito mientras el formulario estaba abierto (la
    /// detección de cambios externos compara sellos temporales). Si la base
    /// acaba de renovarse, la propia ausencia de archivo ya cambia el sello.
    /// </summary>
    public static void TouchClientsFile()
    {
        if (File.Exists(DatabasePath))
        {
            File.SetLastWriteTimeUtc(DatabasePath, DateTime.UtcNow.AddMinutes(5));
        }
    }

    // Renueva la base de datos: se borrará con sus archivos auxiliares y la
    // volverá a crear (migrando los JSON de siembra) la próxima apertura.
    private static void ResetDatabase()
    {
        File.Delete(DatabasePath);
        File.Delete(DatabasePath + "-journal");
        File.Delete(DatabasePath + "-wal");
        File.Delete(DatabasePath + "-shm");
    }
}

/// <summary>
/// Acceso de pruebas a los miembros privados de los formularios (tabla, barra
/// de filtro y manejadores de los botones), para simular clics y ediciones
/// sin ratón. Las excepciones de los manejadores se relanzan sin envolver.
/// </summary>
internal static class FormAccess
{
    public static DataGridView Grid(object form, string fieldName) =>
        (DataGridView)Field(form, fieldName).GetValue(form)!;

    public static TextBox TextBox(object form, string fieldName) =>
        (TextBox)Field(form, fieldName).GetValue(form)!;

    public static ComboBox ComboBox(object form, string fieldName) =>
        (ComboBox)Field(form, fieldName).GetValue(form)!;

    public static Button Button(object form, string fieldName) =>
        (Button)Field(form, fieldName).GetValue(form)!;

    public static Label Label(object form, string fieldName) =>
        (Label)Field(form, fieldName).GetValue(form)!;

    public static string CellText(DataGridViewRow row, string columnName) =>
        row.Cells[columnName].Value?.ToString() ?? string.Empty;

    private static FieldInfo Field(object form, string fieldName) =>
        form.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Campo desconocido: {fieldName}");

    /// <summary>
    /// Invoca el manejador de un botón (clic simulado) y relanza la excepción
    /// real si el formulario falla, para que la prueba señale la causa.
    /// </summary>
    public static void Click(object form, string handlerName)
    {
        MethodInfo method = form.GetType().GetMethod(handlerName, BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Manejador desconocido: {handlerName}");
        try
        {
            method.Invoke(form, new object?[] { null, EventArgs.Empty });
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException!;
        }
    }
}

/// <summary>
/// Sustituye los diálogos del formulario en las pruebas: registra cada mensaje
/// mostrado y responde con las respuestas indicadas, en orden (si se agotan o
/// no se indican, responde Sí/Aceptar).
/// </summary>
internal sealed class PromptRecorder
{
    private readonly Queue<DialogResult> _answers = new();

    public List<string> Messages { get; } = new();

    public List<string> Captions { get; } = new();

    public PromptRecorder(params DialogResult[] answers)
    {
        foreach (DialogResult answer in answers)
        {
            _answers.Enqueue(answer);
        }
    }

    public DialogResult Answer(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
    {
        Messages.Add(text);
        Captions.Add(caption);
        return _answers.Count > 0 ? _answers.Dequeue() : DialogResult.Yes;
    }
}

/// <summary>
/// Registros de prueba para las pruebas de interfaz: DNIs con la letra de
/// control correcta (tabla TRWAGMYFPDXBNJZSQVHLCKE) y tríos CP/ciudad/
/// provincia reales del catálogo (28001 Madrid Madrid).
/// </summary>
internal static class Fixtures
{
    /// <summary>Cliente completo y válido.</summary>
    public static Client Client(string firstName, string taxId,
        ClientStatus status = ClientStatus.Active, string phone = "612345678") => new()
    {
        Id = Guid.NewGuid(),
        FirstName = firstName,
        LastName1 = "García",
        LastName2 = "Pérez",
        TaxId = taxId,
        Address = "Calle Mayor 1",
        PostalCode = "28001",
        City = "Madrid",
        Province = "Madrid",
        Phone = phone,
        Email = "correo@example.com",
        RegistrationDate = new DateTime(2026, 1, 15),
        Status = status,
        Notes = string.Empty
    };

    /// <summary>Futuro cliente mínimo válido: nombre y teléfono.</summary>
    public static Contact Contact(string firstName, string phone = "600111222",
        ContactStatus status = ContactStatus.Pending) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = firstName,
        Phone = phone,
        Status = status
    };

    /// <summary>
    /// Futuro cliente que ya cumple TODAS las reglas de los clientes: se puede
    /// convertir sin carencias.
    /// </summary>
    public static Contact ConvertibleContact(string taxId = "11111111H") => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Clara",
        LastName1 = "Nieto",
        LastName2 = "Vega",
        TaxId = taxId,
        Address = "Calle Mayor 9",
        PostalCode = "28001",
        City = "Madrid",
        Province = "Madrid",
        Phone = "655443322",
        Email = "clara@example.com",
        RegistrationDate = new DateTime(2026, 2, 10),
        Notes = "Visto en la feria del mueble",
        Status = ContactStatus.Pending
    };
}
