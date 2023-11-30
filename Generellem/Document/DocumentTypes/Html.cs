namespace Generellem.Document.DocumentTypes;

public class Html : IDocumentType
{
    public bool CanProcess { get; set; } = false;

    public List<string> SupportedExtensions => new() { ".html", ".htm" };

    public string GetText(Stream documentStream, string fileName) => throw new NotImplementedException();
}
