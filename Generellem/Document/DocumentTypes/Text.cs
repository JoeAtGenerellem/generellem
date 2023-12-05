namespace Generellem.Document.DocumentTypes;
public class Text : IDocumentType
{
    public virtual bool CanProcess { get; set; } = true;

    public virtual List<string> SupportedExtensions => new() { ".txt" };

    public virtual string GetText(Stream documentStream, string fileName)
    {
        using StreamReader reader = new StreamReader(documentStream);
        return reader.ReadToEnd();
    }
}
