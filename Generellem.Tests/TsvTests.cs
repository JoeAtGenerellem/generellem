using Generellem.Document.DocumentTypes;

public class TsvTests
{
    readonly Tsv tsv = new();

    [Fact]
    public void SupportedExtensions_ReturnsTsvExtension()
    {
        var extensions = tsv.SupportedExtensions;

        Assert.Contains(".tsv", extensions);
    }

    [Fact]
    public void SupportedExtensions_ReturnsOnlyTsv()
    {
        var extensions = tsv.SupportedExtensions;

        Assert.Single(extensions);
        Assert.Equal(".tsv", extensions[0]);
    }
}
