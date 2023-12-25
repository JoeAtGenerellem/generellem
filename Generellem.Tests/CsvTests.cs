using Generellem.Document.DocumentTypes;

public class CsvTests
{
    readonly Csv csv = new();

    [Fact]
    public void SupportedExtensions_ReturnsCsvExtension()
    {
        var extensions = csv.SupportedExtensions;

        Assert.Contains(".csv", extensions);
    }

    [Fact]
    public void SupportedExtensions_ReturnsOnlyCsv()
    {
        var extensions = csv.SupportedExtensions;

        Assert.Single(extensions);
        Assert.Equal(".csv", extensions[0]);
    }
}
