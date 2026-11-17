using Crm1.WindowsForms.Services;
using Xunit;

namespace Crm1.Tests;

/// <summary>
/// Pruebas de lectura y escritura de precios en formato español
/// (specs/009 §5): formatos admitidos, rechazos y presentación.
/// </summary>
public sealed class SpanishPriceTests
{
    [Theory]
    [InlineData("1234,56", 1234.56)]
    [InlineData("1.234,56", 1234.56)]
    [InlineData("1234.56", 1234.56)]
    [InlineData("1.234", 1234)]
    [InlineData("1.234.567", 1234567)]
    [InlineData("1.234.567,89", 1234567.89)]
    [InlineData("12", 12)]
    [InlineData(" 9,5 € ", 9.5)]
    [InlineData("1,5", 1.5)]
    [InlineData("0", 0)]
    public void TryParse_admits_spanish_formats(string text, decimal expected)
    {
        Assert.True(SpanishPrice.TryParse(text, out decimal amount));
        Assert.Equal(expected, amount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("12,3,4")]
    [InlineData("1.23.45")]
    public void TryParse_rejects_invalid_text(string text)
    {
        Assert.False(SpanishPrice.TryParse(text, out _));
    }

    [Theory]
    [InlineData(0, "0,00")]
    [InlineData(9.5, "9,50")]
    [InlineData(1234.56, "1.234,56")]
    [InlineData(1234567.89, "1.234.567,89")]
    public void Format_uses_spanish_thousands_and_two_decimals(decimal amount, string expected)
    {
        Assert.Equal(expected, SpanishPrice.Format(amount));
    }
}
