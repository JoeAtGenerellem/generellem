namespace Generellem.Document.DocumentTypes;

public class Image : IDocumentType
{
    public virtual bool CanProcess { get; set; } = false;

    public virtual List<string> SupportedExtensions => new() { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };

    public virtual async Task<string> GetTextAsync(Stream documentStream, string fileName) => await Task.FromResult(string.Empty);
}
