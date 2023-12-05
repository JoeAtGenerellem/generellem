namespace Generellem.Document.DocumentTypes;

public class Unknown : IDocumentType
{
    public virtual bool CanProcess { get; set; } = false;

    public virtual List<string> SupportedExtensions => new();

    public virtual string GetText(Stream documentStream, string fileName) => throw new NotImplementedException();
}
