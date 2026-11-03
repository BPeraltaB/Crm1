namespace Crm1.WindowsForms.Models;

/// <summary>
/// Futuro cliente: persona o empresa en seguimiento antes de convertirse en
/// cliente. Todos los campos son opcionales salvo el nombre y un medio de
/// contacto (teléfono o correo); los datos que faltan se rellenan cuando se
/// conocen (specs/003-formulario-futuros-clientes.md).
/// </summary>
public class Contact
{
    /// <summary>
    /// Identificador único del futuro cliente.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Nombre o razón social (obligatorio).
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Primer apellido (opcional).
    /// </summary>
    public string LastName1 { get; set; } = string.Empty;

    /// <summary>
    /// Segundo apellido (opcional).
    /// </summary>
    public string LastName2 { get; set; } = string.Empty;

    /// <summary>
    /// Documento de identificación: DNI, NIE o CIF (opcional; si está relleno
    /// debe ser válido y no pertenecer a un cliente).
    /// </summary>
    public string TaxId { get; set; } = string.Empty;

    /// <summary>
    /// Dirección (opcional).
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Código postal (5 dígitos como texto; opcional).
    /// </summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Ciudad (opcional).
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Provincia (una de las 52 oficiales; opcional).
    /// </summary>
    public string Province { get; set; } = string.Empty;

    /// <summary>
    /// Teléfono (fijo o móvil español; teléfono o correo son obligatorios).
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Correo electrónico (teléfono o correo son obligatorios).
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de alta (vacía = sin fecha).
    /// </summary>
    public DateTime RegistrationDate { get; set; }

    /// <summary>
    /// Observaciones (opcionales).
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Estado del futuro cliente.
    /// </summary>
    public ContactStatus Status { get; set; } = ContactStatus.Pending;
}
