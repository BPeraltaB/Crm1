namespace Crm1.WindowsForms.Models;

/// <summary>
/// Cliente del CRM con sus datos fiscales y de contacto.
/// </summary>
public class Client
{
    /// <summary>
    /// Identificador único del cliente.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Nombre o razón social (obligatorio).
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Primer apellido (vacío en empresas).
    /// </summary>
    public string LastName1 { get; set; } = string.Empty;

    /// <summary>
    /// Segundo apellido (vacío en empresas y extranjeros con NIE).
    /// </summary>
    public string LastName2 { get; set; } = string.Empty;

    /// <summary>
    /// Documento de identificación: DNI, NIE o CIF (único entre clientes).
    /// </summary>
    public string TaxId { get; set; } = string.Empty;

    /// <summary>
    /// Dirección (obligatoria).
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Código postal (5 dígitos, como texto para conservar los ceros iniciales).
    /// </summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Ciudad (obligatoria).
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Provincia (una de las 52 oficiales).
    /// </summary>
    public string Province { get; set; } = string.Empty;

    /// <summary>
    /// Teléfono móvil (obligatorio).
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Correo electrónico (obligatorio, formato válido).
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de alta del cliente.
    /// </summary>
    public DateTime RegistrationDate { get; set; }

    /// <summary>
    /// Observaciones (único campo opcional).
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Estado del cliente.
    /// </summary>
    public ClientStatus Status { get; set; } = ClientStatus.Active;
}
