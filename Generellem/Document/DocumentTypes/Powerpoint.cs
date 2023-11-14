namespace Generellem.Document.DocumentTypes;

public class Powerpoint : IDocumentType
{
    public bool CanProcess { get; set; } = true;

    public List<string> SupportedExtensions => new() { ".pptx", ".ppt" };

    public string GetText(string path) => throw new NotImplementedException();
}
