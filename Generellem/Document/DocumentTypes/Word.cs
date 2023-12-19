using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NPOI.HWPF.Extractor;
using NPOI.HWPF;

namespace Generellem.Document.DocumentTypes;

public class Word : IDocumentType
{
    public virtual bool CanProcess => true;

    public virtual List<string> SupportedExtensions => new() { ".docx", ".doc" };

    public virtual async Task<string> GetTextAsync(Stream documentStream, string filePath)
    {
        string extension = Path.GetExtension(filePath);

        string text = extension.ToLower() switch
        {
            ".docx" => ReadDocx(filePath),
            ".doc" => ReadDoc(filePath),
            _ => throw new ArgumentException("Unsupported file format"),
        };

        return await Task.FromResult(text);
    }

    private static string ReadDoc(string filePath)
    {
        using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            HWPFDocument doc = new HWPFDocument(fileStream);
            WordExtractor wordExtractor = new WordExtractor(doc);
            return string.Join(Environment.NewLine, wordExtractor.ParagraphText);
        }
    }

    protected virtual string ReadDocx(string filePath)
    {
        using (WordprocessingDocument doc = WordprocessingDocument.Open(filePath, false))
        {
            var body = doc?.MainDocumentPart?.Document?.Body;
            var paragraphs = body?.Elements<Paragraph>();
            return string.Join(Environment.NewLine, paragraphs?.Select(p => p.InnerText) ?? new List<string>());
        }
    }
}
