using Generellem.Document.DocumentTypes;

namespace Generellem.DocTypes.Tests;

public class PowerpointTests
{
    readonly Mock<Stream> streamMock = new();

    readonly Powerpoint powerpoint = new();

    [Fact]
    public async Task GetTextAsync_WithValidFile_ReturnsText()
    {
        const string File = "TestDocs/PowerPointDoc.pptx";
        using Stream stream = System.IO.File.OpenRead(File);

        string text = await powerpoint.GetTextAsync(stream, File);

        Assert.NotNull(text);
        Assert.NotEmpty(text);
    }

    [Fact]
    public async Task GetTextAsync_WithNoText_ReturnsEmpty()
    {
        const string File = "TestDocs/EmptyDeck.pptx";
        using Stream stream = System.IO.File.OpenRead(File);

        string text = await powerpoint.GetTextAsync(stream, File);

        Assert.Empty(text);
    }
}
