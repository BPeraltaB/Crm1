using System.Text.Encodings.Web;
using System.Text.Json;
using Crm1.WindowsForms.Models;

namespace Crm1.WindowsForms.Services;

/// <summary>
/// Almacenamiento JSON: clientes en Data/clients.json y futuros clientes en
/// Data/contacts.json, junto al ejecutable. Archivos independientes.
/// </summary>
public sealed class JsonDataStore : IDataStore
{
    // JSON legible: indentado y con caracteres españoles sin escapar.
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly string _filePath;
    private readonly string _contactsFilePath;

    /// <summary>
    /// Crea el almacén. Sin rutas explícitas usa Data/clients.json y
    /// Data/contacts.json junto al ejecutable.
    /// </summary>
    public JsonDataStore(string? filePath = null, string? contactsFilePath = null)
    {
        _filePath = filePath ?? Path.Combine(AppContext.BaseDirectory, "Data", "clients.json");
        _contactsFilePath = contactsFilePath ?? Path.Combine(AppContext.BaseDirectory, "Data", "contacts.json");
    }

    /// <summary>
    /// Ruta del archivo de clientes.
    /// </summary>
    public string FilePath => _filePath;

    /// <summary>
    /// Ruta del archivo de futuros clientes.
    /// </summary>
    public string ContactsFilePath => _contactsFilePath;

    /// <inheritdoc/>
    public List<Client> LoadClients() => Load<Client>(_filePath, "clientes");

    /// <inheritdoc/>
    public void SaveClients(List<Client> clients) => Save(_filePath, clients, "clientes");

    /// <inheritdoc/>
    public List<Contact> LoadContacts() => Load<Contact>(_contactsFilePath, "futuros clientes");

    /// <inheritdoc/>
    public void SaveContacts(List<Contact> contacts) => Save(_contactsFilePath, contacts, "futuros clientes");

    // Lee una lista de un archivo JSON. Un archivo inexistente es una primera
    // ejecución (lista vacía); un archivo ilegible lanza un error visible
    // (constitución, punto 5).
    private static List<T> Load<T>(string filePath, string description)
    {
        // Un archivo inexistente es una primera ejecución: lista vacía.
        if (!File.Exists(filePath))
        {
            return new List<T>();
        }

        try
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
        }
        catch (Exception ex)
        {
            // Un fallo de lectura nunca se presenta como éxito (constitución, punto 5).
            throw new InvalidOperationException($"No se pudo leer el archivo de {description} '{filePath}'.", ex);
        }
    }

    // Guarda una lista en un archivo JSON de forma atómica: primero a un
    // archivo temporal y luego se reemplaza, para no perder el archivo anterior
    // si la escritura falla a mitad.
    private static void Save<T>(string filePath, List<T> items, string description)
    {
        try
        {
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string tempPath = filePath + ".tmp";
            string json = JsonSerializer.Serialize(items, SerializerOptions);
            File.WriteAllText(tempPath, json);

            if (File.Exists(filePath))
            {
                File.Replace(tempPath, filePath, null);
            }
            else
            {
                File.Move(tempPath, filePath);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"No se pudo guardar el archivo de {description} '{filePath}'.", ex);
        }
    }
}
