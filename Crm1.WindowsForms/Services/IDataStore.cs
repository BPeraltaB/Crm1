using Crm1.WindowsForms.Models;

namespace Crm1.WindowsForms.Services;

/// <summary>
/// Contrato de almacenamiento de los datos de la aplicación.
/// </summary>
public interface IDataStore
{
    /// <summary>
    /// Carga los clientes guardados. Un archivo inexistente devuelve una lista vacía
    /// (primera ejecución); un archivo ilegible lanza una excepción visible
    /// (constitución, punto 5).
    /// </summary>
    List<Client> LoadClients();

    /// <summary>
    /// Guarda los clientes de forma atómica (archivo temporal y reemplazo).
    /// </summary>
    void SaveClients(List<Client> clients);

    /// <summary>
    /// Carga los futuros clientes guardados. Un archivo inexistente devuelve una
    /// lista vacía (primera ejecución); un archivo ilegible lanza una excepción
    /// visible (constitución, punto 5).
    /// </summary>
    List<Contact> LoadContacts();

    /// <summary>
    /// Guarda los futuros clientes de forma atómica (archivo temporal y reemplazo).
    /// </summary>
    void SaveContacts(List<Contact> contacts);
}

