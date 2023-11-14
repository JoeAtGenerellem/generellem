namespace Generellem.Document.DocumentTypes;

public class Video : IDocumentType
{
    public bool CanProcess { get; set; } = false;

    public List<string> SupportedExtensions => new() { ".mp4", ".avi", ".mkv" };

    public string GetText(string path) => throw new NotImplementedException();
}
