namespace Generellem.Document.DocumentTypes;

public class Image : IDocumentType
{
    public bool CanProcess { get; set; } = false;

    public List<string> SupportedExtensions => new() { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };

    public string GetText(Stream documentStream, string fileName) => throw new NotImplementedException();
}
