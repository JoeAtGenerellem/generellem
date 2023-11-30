namespace Generellem.Document.DocumentTypes;

public class Audio : IDocumentType
{
    public bool CanProcess { get; set; } = false;

    public List<string> SupportedExtensions => new() { ".mp3", ".wav", ".ogg" };

    public string GetText(Stream documentStream, string fileName) => throw new NotImplementedException();
}
