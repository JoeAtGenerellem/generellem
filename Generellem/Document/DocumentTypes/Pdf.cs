using UglyToad.PdfPig;

namespace Generellem.Document.DocumentTypes;

public class Pdf : IDocumentType
{
    public virtual bool CanProcess => true;

    public virtual List<string> SupportedExtensions => new() { ".pdf" };

    public virtual async Task<string> GetTextAsync(Stream documentStream, string fileName)
    {
        ArgumentNullException.ThrowIfNull(documentStream, nameof(documentStream));
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));

        if (!File.Exists(fileName))
            throw new FileNotFoundException("File not found", fileName);

        using PdfDocument document = PdfDocument.Open(documentStream);

        List<string> pages =
            (from page in document.GetPages()
             select page.Text)
            .ToList();

        return await Task.FromResult(string.Join(' ', pages));
    }
}
