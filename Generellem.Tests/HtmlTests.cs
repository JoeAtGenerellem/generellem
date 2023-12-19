using System.Text;

using Generellem.Document.DocumentTypes;

namespace Generellem.DocTypes.Tests;

public class HtmlTests
{
    readonly Html html = new();

    [Fact]
    public async Task GetTextAsync_ExtractsAllText()
    {
        var html = "<html><body><p>Hello <span>World</span></p></body></html>";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));

        var result = await this.html.GetTextAsync(stream, "test.html");

        Assert.Equal("Hello World", result);
    }

    [Fact]
    public async Task GetTextAsync_HandlesMalformedHtml()
    {
        var html = "<html><body><p>Hello World</body></html>";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));

        var result = await this.html.GetTextAsync(stream, "test.html");

        Assert.Equal("Hello World", result);
    }

    [Fact]
    public async Task GetTextAsync_HandlesEmptyHtml()
    {
        var html = "<html><body></body></html>";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));

        var result = await this.html.GetTextAsync(stream, "test.html");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void CanProcess_IsTrue()
    {
        Assert.True(html.CanProcess);
    }
}
