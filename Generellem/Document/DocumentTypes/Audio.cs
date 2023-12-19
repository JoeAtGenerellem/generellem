namespace Generellem.Document.DocumentTypes;

public class Audio : IDocumentType
{
    public virtual bool CanProcess => false;

    public virtual List<string> SupportedExtensions => new() { ".mp3", ".wav", ".ogg" };

    public virtual async Task<string> GetTextAsync(Stream documentStream, string fileName) => await Task.FromResult(string.Empty);
}
