namespace Generellem.Document.DocumentTypes;

public class Powerpoint : IDocumentType
{
    public virtual bool CanProcess => false;

    public virtual List<string> SupportedExtensions => new() { ".pptx", ".ppt" };

    public virtual async Task<string> GetTextAsync(Stream documentStream, string fileName) => await Task.FromResult(string.Empty);
}
