namespace Generellem.Document.DocumentTypes;

public class Pdf : IDocumentType
{
    public bool CanProcess { get; set; } = false;

    public List<string> SupportedExtensions => new() { ".pdf" };

    public string GetText(Stream documentStream, string fileName) => throw new NotImplementedException();
}
