namespace Generellem.Document.DocumentTypes;

public class Executable : IDocumentType
{
    public virtual bool CanProcess { get; set; } = false;

    public virtual List<string> SupportedExtensions => new() { ".exe", ".dll" };

    public virtual string GetText(Stream documentStream, string fileName) => throw new NotImplementedException();
}
