namespace Crm1.WindowsForms.Models;

/// <summary>
/// Tipo de documento de identificación fiscal español.
/// </summary>
public enum DocumentType
{
    /// <summary>El formato no se reconoce como documento válido.</summary>
    Unknown,

    /// <summary>DNI: 8 dígitos y letra de control.</summary>
    Dni,

    /// <summary>NIE: X/Y/Z, 7 dígitos y letra de control.</summary>
    Nie,

    /// <summary>CIF: letra de organización, 7 dígitos y carácter de control.</summary>
    Cif
}
