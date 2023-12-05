namespace Generellem.Document.DocumentTypes;

public class Word : IDocumentType
{
    public virtual bool CanProcess { get; set; } = false;

    public virtual List<string> SupportedExtensions => new() { ".docx", ".doc" };

    public virtual string GetText(Stream documentStream, string fileName) => throw new NotImplementedException();
}
