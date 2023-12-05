namespace Generellem.Document.DocumentTypes;

public class Pdf : IDocumentType
{
    public virtual bool CanProcess { get; set; } = false;

    public virtual List<string> SupportedExtensions => new() { ".pdf" };

    public virtual string GetText(Stream documentStream, string fileName) => throw new NotImplementedException();
}
