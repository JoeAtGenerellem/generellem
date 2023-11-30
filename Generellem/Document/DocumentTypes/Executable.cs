namespace Generellem.Document.DocumentTypes;

public class Executable : IDocumentType
{
    public bool CanProcess { get; set; } = false;

    public List<string> SupportedExtensions => new() { ".exe", ".dll" };

    public string GetText(Stream documentStream, string fileName) => throw new NotImplementedException();
}
