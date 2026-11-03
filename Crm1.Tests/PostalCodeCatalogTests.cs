using System.Text;
using Crm1.WindowsForms.Services;
using Xunit;

namespace Crm1.Tests;

/// <summary>
/// Pruebas del catálogo de códigos postales con un archivo de datos reducido propio.
/// </summary>
public sealed class PostalCodeCatalogTests : IDisposable
{
    private readonly string _catalogPath;

    public PostalCodeCatalogTests()
    {
        _catalogPath = Path.Combine(
            Path.GetTempPath(), "opencode", "crm1-tests", $"catalog-{Guid.NewGuid():N}.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_catalogPath)!);
        File.WriteAllText(_catalogPath,
            "[{\"postalCode\":\"01001\",\"municipalities\":[\"Vitoria-Gasteiz\"]}," +
            "{\"postalCode\":\"01002\",\"municipalities\":[\"Vitoria-Gasteiz\"]}," +
            "{\"postalCode\":\"01010\",\"municipalities\":[\"Alegría-Dulantzi\"]}," +
            "{\"postalCode\":\"03801\",\"municipalities\":[\"Alcoy/Alcoi\"]}," +
            "{\"postalCode\":\"28001\",\"municipalities\":[\"Madrid\"]}," +
            "{\"postalCode\":\"28002\",\"municipalities\":[\"Madrid\"]}," +
            "{\"postalCode\":\"99999\",\"municipalities\":[]}]",
            Encoding.UTF8);
    }

    public void Dispose()
    {
        File.Delete(_catalogPath);
    }

    private PostalCodeCatalog CreateCatalog() => new(_catalogPath);

    [Fact]
    public void Provinces_contains_the_52_official_provinces()
    {
        PostalCodeCatalog catalog = CreateCatalog();

        Assert.Equal(52, catalog.Provinces.Count);
        Assert.Contains("Araba/Álava", catalog.Provinces);
        Assert.Contains("Madrid", catalog.Provinces);
        Assert.Contains("Santa Cruz de Tenerife", catalog.Provinces);
        Assert.Contains("Melilla", catalog.Provinces);
    }

    [Fact]
    public void Exists_checks_the_catalog_entries()
    {
        PostalCodeCatalog catalog = CreateCatalog();

        Assert.True(catalog.Exists("01001"));
        Assert.True(catalog.Exists("99999"));
        Assert.False(catalog.Exists("88888"));
    }

    [Fact]
    public void Municipalities_are_returned_by_postal_code()
    {
        PostalCodeCatalog catalog = CreateCatalog();

        Assert.Equal(new[] { "Vitoria-Gasteiz" }, catalog.GetMunicipalities("01001"));
        Assert.Empty(catalog.GetMunicipalities("88888"));
    }

    [Theory]
    [InlineData("Vitoria-Gasteiz", "01001", true)]
    [InlineData("vitoria", "01001", true)]        // forma doble, minúsculas
    [InlineData("Gasteiz", "01001", true)]        // segunda forma del nombre oficial
    [InlineData("Madrid", "01001", false)]
    [InlineData("Alcoy", "03801", true)]          // forma doble con barra
    [InlineData("Alcoi", "03801", true)]
    public void CityMatchesPostalCode_compares_names_without_accents_or_case(
        string city, string postalCode, bool expected)
    {
        PostalCodeCatalog catalog = CreateCatalog();

        Assert.Equal(expected, catalog.CityMatchesPostalCode(city, postalCode));
    }

    [Theory]
    [InlineData("madrid", "Madrid", true)]
    [InlineData("MADRID", "Madrid", true)]
    [InlineData("vitoria", "Araba/Álava", true)]
    [InlineData("madrid", "Barcelona", false)]
    [InlineData("leganes", "Madrid", false)]      // no está en el catálogo reducido
    public void CityBelongsToProvince_uses_the_catalog(string city, string province, bool expected)
    {
        PostalCodeCatalog catalog = CreateCatalog();

        Assert.Equal(expected, catalog.CityBelongsToProvince(city, province));
    }

    [Theory]
    [InlineData("01001", "Araba/Álava")]
    [InlineData("28001", "Madrid")]
    [InlineData("51001", "Ceuta")]
    [InlineData("52001", "Melilla")]
    [InlineData("00001", null)]
    [InlineData("53001", null)]
    [InlineData("abc", null)]
    public void GetProvinceName_uses_the_postal_code_prefix(string postalCode, string? expected)
    {
        Assert.Equal(expected, PostalCodeCatalog.GetProvinceName(postalCode));
    }

    [Fact]
    public void Missing_catalog_file_throws_a_visible_error()
    {
        string missingPath = Path.Combine(
            Path.GetTempPath(), "opencode", "crm1-tests", $"missing-{Guid.NewGuid():N}.json");

        Assert.Throws<InvalidOperationException>(() => new PostalCodeCatalog(missingPath));
    }
}
