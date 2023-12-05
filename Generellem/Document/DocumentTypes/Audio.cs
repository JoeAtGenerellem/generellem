namespace Generellem.Document.DocumentTypes;

public class Audio : IDocumentType
{
    public virtual bool CanProcess { get; set; } = false;

    public virtual List<string> SupportedExtensions => new() { ".mp3", ".wav", ".ogg" };

    public virtual string GetText(Stream documentStream, string fileName) => throw new NotImplementedException();
}
