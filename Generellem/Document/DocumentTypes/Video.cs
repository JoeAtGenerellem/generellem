namespace Generellem.Document.DocumentTypes;

public class Video : IDocumentType
{
    public virtual bool CanProcess { get; set; } = false;

    public virtual List<string> SupportedExtensions => new() { ".mp4", ".avi", ".mkv" };

    public virtual string GetText(Stream documentStream, string fileName) => throw new NotImplementedException();
}
