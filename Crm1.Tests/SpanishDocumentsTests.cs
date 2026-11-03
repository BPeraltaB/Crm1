using Crm1.WindowsForms.Models;
using Crm1.WindowsForms.Services;
using Xunit;

namespace Crm1.Tests;

/// <summary>
/// Pruebas del validador de documentos DNI, NIE y CIF.
/// Los ejemplos válidos están calculados con los algoritmos oficiales de control.
/// </summary>
public class SpanishDocumentsTests
{
    [Theory]
    [InlineData("12345678Z", DocumentType.Dni)]   // 12345678 % 23 = 14 → Z
    [InlineData("00000000T", DocumentType.Dni)]   // 0 % 23 = 0 → T
    [InlineData("X1234567L", DocumentType.Nie)]   // X→0: 1234567 % 23 = 19 → L
    [InlineData("Z1234567R", DocumentType.Nie)]   // Z→2: 21234567 % 23 = 1 → R
    [InlineData("A46103834", DocumentType.Cif)]   // control dígito 4 (A exige dígito)
    public void Documents_with_valid_control_are_accepted(string document, DocumentType expectedType)
    {
        Assert.True(SpanishDocuments.IsValid(document, out DocumentType type));
        Assert.Equal(expectedType, type);
    }

    [Theory]
    [InlineData("12345678A")]     // DNI con letra de control incorrecta
    [InlineData("X1234567X")]     // NIE con letra de control incorrecta
    [InlineData("A46103835")]     // CIF con control incorrecto
    [InlineData("1234567")]       // sin letra de control
    [InlineData("A123456789")]    // demasiado largo
    [InlineData("4610383")]       // solo dígitos
    [InlineData("hello")]         // sin formato de documento
    [InlineData("")]
    public void Documents_with_invalid_format_or_control_are_rejected(string document)
    {
        // Se rechaza tanto por formato como por carácter de control incorrecto.
        Assert.False(SpanishDocuments.IsValid(document, out _));
    }

    [Theory]
    [InlineData("12345678A", DocumentType.Dni)]   // formato válido, control incorrecto
    [InlineData("X1234567X", DocumentType.Nie)]
    [InlineData("A46103835", DocumentType.Cif)]
    public void Wrong_control_is_rejected_but_the_type_is_detected(string document, DocumentType expectedType)
    {
        Assert.False(SpanishDocuments.IsValid(document, out DocumentType type));
        Assert.Equal(expectedType, type);
    }

    [Theory]
    [InlineData("1234567")]
    [InlineData("4610383")]
    [InlineData("hello")]
    [InlineData("")]
    public void Unrecognizable_documents_have_unknown_type(string document)
    {
        Assert.Equal(DocumentType.Unknown, SpanishDocuments.DetectType(document));
    }

    [Fact]
    public void Dni_with_seven_digits_is_normalized_with_leading_zero()
    {
        // 2345678 % 23 = 0 → T; el DNI de 7 dígitos se rellena con un cero.
        Assert.Equal("02345678T", SpanishDocuments.Normalize(" 2345678t "));
        Assert.True(SpanishDocuments.IsValid("2345678t", out DocumentType type));
        Assert.Equal(DocumentType.Dni, type);
    }

    [Fact]
    public void Document_is_normalized_to_uppercase_without_spaces()
    {
        Assert.Equal("12345678Z", SpanishDocuments.Normalize(" 12345678z "));
    }
}
