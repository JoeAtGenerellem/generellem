namespace Generellem.Document.DocumentTypes;

public class Excel : IDocumentType
{
    public virtual bool CanProcess { get; set; } = false;

    public virtual List<string> SupportedExtensions => new() { ".xlsx", ".xls" };

    public virtual async Task<string> GetTextAsync(Stream documentStream, string fileName) => await Task.FromResult(string.Empty);
}
