namespace Generellem.Document.DocumentTypes;
public class Text : IDocumentType
{
    public bool CanProcess { get; set; } = true;

    public virtual List<string> SupportedExtensions => new() { ".txt" };

    public string GetText(Stream documentStream, string fileName)
    {
        using StreamReader reader = new StreamReader(documentStream);
        return reader.ReadToEnd();
    }
}
