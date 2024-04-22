using Generellem.Document.DocumentTypes;

namespace Generellem.DocTypes.Tests;

public class TextTests
{
    readonly Text text = new();

    [Fact]
    public void CanProcess_ReturnsTrue()
    {
        Assert.True(text.CanProcess);
    }

    [Fact]
    public void SupportedExtensions_ContainsTxt()
    {
        Assert.Contains(".txt", text.SupportedExtensions);
    }

    [Fact]
    public async Task GetTextAsync_ReturnsFileContents()
    {
        const string TestFileName = "TestDocs/file2.txt";
        using var stream = File.OpenRead(TestFileName);

        string result = await text.GetTextAsync(stream, TestFileName);

        Assert.Equal("Test file", result);
    }

    [Fact]
    public void CanProcess_IsTrue()
    {
        Assert.True(text.CanProcess);
    }
}
