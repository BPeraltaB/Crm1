using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using Crm1.WindowsForms.Models;

namespace Crm1.WindowsForms.Services
{
    // Resultado de una validación de negocio
    public class ValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<string> Errors { get; } = new List<string>();

        public void AddError(string message) => Errors.Add(message);
    }

    // Reglas de negocio y operaciones de clientes, fuera de los formularios
    public class ClientService
    {
        private const string StorageKey = "clients";
        private readonly IDataStore _store;

        public ClientService(IDataStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        // Devuelve todos los clientes registrados
        public List<Client> GetAll() => _store.Load<Client>(StorageKey);

        // Valida y añade un cliente nuevo
        public ValidationResult Add(Client client)
        {
            ValidationResult result = Validate(client);
            if (!result.IsValid)
                return result;

            List<Client> clients = GetAll();
            clients.Add(client);
            _store.Save(StorageKey, clients);
            return result;
        }

        // Valida y actualiza un cliente existente
        public ValidationResult Update(Client client)
        {
            ValidationResult result = Validate(client);
            if (!result.IsValid)
                return result;

            List<Client> clients = GetAll();
            int index = clients.FindIndex(c => c.Id == client.Id);
            if (index < 0)
            {
                result.AddError("El cliente ya no existe.");
                return result;
            }

            clients[index] = client;
            _store.Save(StorageKey, clients);
            return result;
        }

        // Elimina un cliente por identificador; devuelve false si no existía
        public bool Delete(Guid id)
        {
            List<Client> clients = GetAll();
            int removed = clients.RemoveAll(c => c.Id == id);
            if (removed == 0)
                return false;

            _store.Save(StorageKey, clients);
            return true;
        }

        // Búsqueda por texto libre (nombre, CIF/NIF, ciudad o provincia) y filtro opcional por estado
        public List<Client> Search(string text, ClientStatus? status = null)
        {
            IEnumerable<Client> query = GetAll();

            if (status.HasValue)
                query = query.Where(c => c.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(text))
            {
                string filter = text.Trim();
                query = query.Where(c =>
                    Contains(c.Name, filter) ||
                    Contains(c.TaxId, filter) ||
                    Contains(c.City, filter) ||
                    Contains(c.Province, filter));
            }

            return query.ToList();
        }

        // Reglas de negocio comunes para alta y edición
        private ValidationResult Validate(Client client)
        {
            var result = new ValidationResult();

            if (client == null)
            {
                result.AddError("Falta el dato del cliente.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(client.Name))
                result.AddError("El nombre es obligatorio.");

            if (!string.IsNullOrWhiteSpace(client.Email) && !IsValidEmail(client.Email))
                result.AddError("El correo electrónico no tiene un formato válido.");

            if (!string.IsNullOrWhiteSpace(client.TaxId) && TaxIdInUse(client))
                result.AddError("Ya existe un cliente con ese CIF/NIF.");

            return result;
        }

        // Comprueba si el CIF/NIF ya lo usa otro cliente
        private bool TaxIdInUse(Client client)
        {
            string normalized = client.TaxId?.Trim() ?? string.Empty;
            return GetAll().Any(c =>
                c.Id != client.Id &&
                string.Equals(c.TaxId?.Trim(), normalized, StringComparison.OrdinalIgnoreCase));
        }

        // Coincidencia parcial sin mayúsculas/minúsculas
        private static bool Contains(string? source, string filter) =>
            source?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false;

        // Validación básica de formato de correo electrónico
        private static bool IsValidEmail(string email)
        {
            try
            {
                _ = new MailAddress(email);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
