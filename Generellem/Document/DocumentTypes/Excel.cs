namespace Generellem.Document.DocumentTypes;

public class Excel : IDocumentType
{
    public bool CanProcess { get; set; } = false;

    public List<string> SupportedExtensions => new() { ".xlsx", ".xls" };

    public string GetText(string path) => throw new NotImplementedException();
}
