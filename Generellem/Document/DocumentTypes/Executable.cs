namespace Generellem.Document.DocumentTypes;

public class Executable : IDocumentType
{
    public virtual bool CanProcess => false;

    public virtual List<string> SupportedExtensions => new() { ".exe", ".dll" };

    public virtual async Task<string> GetTextAsync(Stream documentStream, string fileName) => await Task.FromResult(string.Empty);
}
