namespace Generellem.Document.DocumentTypes;

public class Unknown : IDocumentType
{
    public bool CanProcess { get; set; } = false;

    public List<string> SupportedExtensions => new();

    public string GetText(Stream documentStream, string fileName) => throw new NotImplementedException();
}
