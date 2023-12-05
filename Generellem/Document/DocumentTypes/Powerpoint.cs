namespace Generellem.Document.DocumentTypes;

public class Powerpoint : IDocumentType
{
    public virtual bool CanProcess { get; set; } = false;

    public virtual List<string> SupportedExtensions => new() { ".pptx", ".ppt" };

    public virtual string GetText(Stream documentStream, string fileName) => throw new NotImplementedException();
}
