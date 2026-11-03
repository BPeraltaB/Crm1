using Crm1.WindowsForms.Services;
using Xunit;

namespace Crm1.Tests
{
    // Pruebas de la corrección de mayúsculas al guardar (specs/002 y specs/003).
    public class TextNormalizerTests
    {
        [Theory]
        [InlineData("juan", "Juan")]
        [InlineData("Juan", "Juan")]
        [InlineData("ÓSCAR", "Óscar")]
        [InlineData("maría DE LA torre", "María de la Torre")]
        [InlineData("De la cruz", "De la Cruz")]
        [InlineData("talleres muñoz sa", "Talleres Muñoz SA")]
        [InlineData("construcciones peralta sl", "Construcciones Peralta SL")]
        [InlineData("las palmas", "Las Palmas")]
        [InlineData("las palmas de gran canaria", "Las Palmas de Gran Canaria")]
        [InlineData("gran vía 21", "Gran Vía 21")]
        [InlineData("paseo DE la castellana 12", "Paseo de la Castellana 12")]
        [InlineData("el prado y la fuente", "El Prado y la Fuente")]
        public void CapitalizeWords_CorrectsFirstLetterOfEachWord(string input, string expected)
        {
            Assert.Equal(expected, TextNormalizer.CapitalizeWords(input));
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("   ", "")]
        public void CapitalizeWords_EmptyText_ReturnsEmpty(string input, string expected)
        {
            Assert.Equal(expected, TextNormalizer.CapitalizeWords(input));
        }

        [Fact]
        public void CapitalizeWords_CollapsesExtraSpaces()
        {
            Assert.Equal("Juan García", TextNormalizer.CapitalizeWords("  juan    garcía  "));
        }

        [Theory]
        [InlineData("cliente desde la apertura.", "Cliente desde la apertura.")]
        [InlineData("Cliente desde la apertura.", "Cliente desde la apertura.")]
        [InlineData("prefiere llamadas por la mañana", "Prefiere llamadas por la mañana")]
        public void CapitalizeSentence_CorrectsOnlyFirstLetter(string input, string expected)
        {
            Assert.Equal(expected, TextNormalizer.CapitalizeSentence(input));
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("   ", "")]
        public void CapitalizeSentence_EmptyText_ReturnsEmpty(string input, string expected)
        {
            Assert.Equal(expected, TextNormalizer.CapitalizeSentence(input));
        }

        [Fact]
        public void CapitalizeSentence_KeepsRestAsTyped()
        {
            // Solo corrige la primera letra: el resto no se toca.
            Assert.Equal("Hola, esto es UNA nota.", TextNormalizer.CapitalizeSentence("hola, esto es UNA nota."));
        }
    }
}
