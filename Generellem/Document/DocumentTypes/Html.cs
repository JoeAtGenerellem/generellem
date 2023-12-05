namespace Generellem.Document.DocumentTypes;

public class Html : IDocumentType
{
    public virtual bool CanProcess { get; set; } = false;

    public virtual List<string> SupportedExtensions => new() { ".html", ".htm" };

    public virtual string GetText(Stream documentStream, string fileName) => throw new NotImplementedException();
}
