using System.Reflection;

using Generellem.Document.DocumentTypes;

namespace Generellem.Document;

public class DocumentTypeFactory
{
    static IEnumerable<string>? supportedDocTypes = null;

    public static IDocumentType Create(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return extension switch
        {
            ".adoc" or ".asc" or ".asciidoc" => new AsciiDoc(),
            ".docx" or ".doc" => new Word(),
            ".html" => new Html(),
            ".md" => new Markdown(),
            ".pdf" => new Pdf(),
            //".pptx" or ".ppt" => new Powerpoint(),
            ".txt" => new Text(),
            //".xls" or ".xlsx" => new Excel(),
            _ => new Unknown(),
        };
    }

    public static IEnumerable<string> GetSupportedDocumentTypes()
    {
        if (supportedDocTypes is null)
            supportedDocTypes = 
                (from docType in Assembly.GetExecutingAssembly().GetTypes()
                 where docType.GetInterfaces().Contains(typeof(IDocumentType))
                 let docTypeInstance = Activator.CreateInstance(docType) as IDocumentType
                 where docTypeInstance.CanProcess
                 from docExtension in docTypeInstance.SupportedExtensions
                 select docExtension)
                .ToList();

        return supportedDocTypes ?? new List<string>();
    }
}