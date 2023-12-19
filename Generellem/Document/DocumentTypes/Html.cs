namespace Generellem.Document.DocumentTypes;

public class Html : IDocumentType
{
    public virtual bool CanProcess => true;

    public virtual List<string> SupportedExtensions => new() { ".html", ".htm" };

    /// <summary>
    /// Parses the given HTML document stream and extracts all text content.
    /// </summary>
    /// <param name="documentStream">The stream containing the HTML document.</param>
    /// <param name="fileName">The file name of the HTML document.</param>
    /// <returns>The extracted text content from the HTML document.</returns>
    public virtual async Task<string> GetTextAsync(Stream documentStream, string fileName)
    {
        using var reader = new StreamReader(documentStream);
        var htmlContent = await reader.ReadToEndAsync();

        // The HtmlAgilityPack library is used to parse the HTML content.
        // It supports HTML versions up to HTML5. If a future version of HTML is released,
        // this code may need to be updated to support it.
        var htmlDocument = new HtmlAgilityPack.HtmlDocument();
        htmlDocument.LoadHtml(htmlContent);

        // Extracting all the text within the HTML tags.
        var allText = htmlDocument.DocumentNode.DescendantsAndSelf()
            .Where(n => n.NodeType == HtmlAgilityPack.HtmlNodeType.Text)
            .Select(n => n.InnerText.Trim())
            .Where(t => !string.IsNullOrEmpty(t));

        return string.Join(" ", allText);
    }
}
