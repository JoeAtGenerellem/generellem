namespace Generellem.Document.DocumentTypes;

public class Word : IDocumentType
{
    public bool CanProcess { get; set; } = false;

    public List<string> SupportedExtensions => new() { ".docx", ".doc" };

    public string GetText(Stream documentStream, string fileName) => throw new NotImplementedException();
}
