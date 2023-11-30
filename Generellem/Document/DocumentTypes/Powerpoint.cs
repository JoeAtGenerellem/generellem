namespace Generellem.Document.DocumentTypes;

public class Powerpoint : IDocumentType
{
    public bool CanProcess { get; set; } = false;

    public List<string> SupportedExtensions => new() { ".pptx", ".ppt" };

    public string GetText(Stream documentStream, string fileName) => throw new NotImplementedException();
}
