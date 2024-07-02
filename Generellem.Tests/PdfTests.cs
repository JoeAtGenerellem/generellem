using Generellem.Document.DocumentTypes;

namespace Generellem.DocTypes.Tests;

public class PdfTests
{
    const string PdfFileContents = "Test Documentation ";

    readonly Mock<Stream> streamMock = new();

    readonly Pdf pdf = new();

    [Fact]
    public async Task GetTextAsync_WithValidPdf_ReturnsText()
    {
        const string FilePath = "TestDocs/PdfDoc1.pdf";
        using Stream stream = File.OpenRead(FilePath);
        var pdf = new Pdf();

        string result = await pdf.GetTextAsync(stream, FilePath);

        Assert.NotNull(result);
        Assert.Equal(PdfFileContents, result);
    }

    [Fact]
    public async Task GetTextAsync_WithNullStream_ThrowsException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => pdf.GetTextAsync(null, ""));
    }

    [Fact]
    public async Task GetTextAsync_WithInvalidFileName_ThrowsException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => pdf.GetTextAsync(streamMock.Object, ""));
    }

    [Fact]
    public void CanProcess_IsTrue()
    {
        Assert.True(pdf.CanProcess);
    }
}
