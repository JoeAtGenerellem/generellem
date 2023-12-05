namespace Generellem.Document.DocumentTypes;

public class Excel : IDocumentType
{
    public virtual bool CanProcess { get; set; } = false;

    public virtual List<string> SupportedExtensions => new() { ".xlsx", ".xls" };

    public virtual string GetText(Stream documentStream, string fileName) => throw new NotImplementedException();
}
