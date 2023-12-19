namespace Generellem.Document.DocumentTypes;

public class Text : IDocumentType
{
    public virtual bool CanProcess => true;

    public virtual List<string> SupportedExtensions => new() { ".txt" };

    public virtual async Task<string> GetTextAsync(Stream documentStream, string fileName)
    {
        using StreamReader reader = new StreamReader(documentStream);
        return await reader.ReadToEndAsync();
    }
}
