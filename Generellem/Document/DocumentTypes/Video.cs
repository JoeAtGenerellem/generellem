namespace Generellem.Document.DocumentTypes;

public class Video : IDocumentType
{
    public virtual bool CanProcess => false;

    public virtual List<string> SupportedExtensions => new() { ".mp4", ".avi", ".mkv" };

    public virtual async Task<string> GetTextAsync(Stream documentStream, string fileName) => await Task.FromResult(string.Empty);
}
