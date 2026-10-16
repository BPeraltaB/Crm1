using System;

namespace Crm1.WindowsForms.Models
{
    // Estado posible de un cliente
    public enum ClientStatus
    {
        Active,
        Inactive
    }

    // Entidad que representa un cliente del CRM
    public class Client
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string TaxId { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
        public string Notes { get; set; } = string.Empty;
        public ClientStatus Status { get; set; } = ClientStatus.Active;
    }
}
