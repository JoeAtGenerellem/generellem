using System.Text;

using Generellem.Document.DocumentTypes;

namespace Generellem.DocTypes.Tests;

public class WordTests
{
    readonly string DocXFileContents = $"Test{Environment.NewLine}Documentation";
    readonly string DocFileContents = $"Test\r\n{Environment.NewLine}Documentation\r\n";

    readonly Mock<Stream> streamMock = new();

    readonly Word word = new();

    public WordTests()
    {
        streamMock.Setup(x => x.Read(It.IsAny<byte[]>(), 0, It.IsAny<int>())).Returns(1);

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    [Fact]
    public async Task GetTextAsync_Docx_ReturnsText()
    {
        var result = await word.GetTextAsync(streamMock.Object, "TestDocs/WordDoc1.docx");

        Assert.Equal(DocXFileContents, result);
    }

    [Fact]
    public async Task GetTextAsync_Doc_ReturnsText()
    {
        var result = await word.GetTextAsync(streamMock.Object, "TestDocs/WordDoc2.doc");

        Assert.Equal(DocFileContents, result);
    }

    [Fact]
    public async Task GetTextAsync_InvalidExtension_ThrowsException()
    {
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await word.GetTextAsync(streamMock.Object, "test.invalid"));

        Assert.Equal("Unsupported file format", exception.Message);
    }

    [Fact]
    public void CanProcess_IsTrue()
    {
        Assert.True(word.CanProcess);
    }
}
