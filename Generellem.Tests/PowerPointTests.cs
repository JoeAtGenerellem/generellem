using Generellem.Document.DocumentTypes;

namespace Generellem.DocTypes.Tests;

public class PowerpointTests
{
    readonly Mock<Stream> streamMock = new();

    readonly Powerpoint powerpoint = new();

    [Fact]
    public async Task GetTextAsync_WithValidFile_ReturnsText()
    {
        var text = await powerpoint.GetTextAsync(streamMock.Object, "TestDocs/PowerPointDoc.pptx");

        Assert.NotNull(text);
        Assert.NotEmpty(text);
    }

    [Fact]
    public async Task GetTextAsync_WithInvalidFile_ThrowsException()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            powerpoint.GetTextAsync(streamMock.Object, "TestDocs/Invalid.pptx"));
    }

    [Fact]
    public async Task GetTextAsync_WithNoText_ReturnsEmpty()
    {
        var text = await powerpoint.GetTextAsync(streamMock.Object, "TestDocs/EmptyDeck.pptx");

        Assert.Empty(text);
    }
}
