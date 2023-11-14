using System.Reflection;

using Generellem.Document.DocumentTypes;

namespace Generellem.Document;

public class DocumentTypeFactory
{
    public static IDocumentType Create(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return extension switch
        {
            ".html" => new Html(),
            ".md" => new Markdown(),
            ".pdf" => new Pdf(),
            ".pptx" or ".ppt" => new Powerpoint(),
            ".txt" => new Text(),
            ".docx" or ".doc" => new Word(),
            _ => new Unknown(),
        };
    }

    public static IEnumerable<string> GetSupportedDocumentTypes()
    {
        var types = 
            (from docType in Assembly.GetExecutingAssembly().GetTypes()
             where docType.GetInterfaces().Contains(typeof(IDocumentType))
             let docTypeInstance = Activator.CreateInstance(docType) as IDocumentType
             where docTypeInstance.CanProcess
             from docExtension in docTypeInstance.SupportedExtensions
             select docExtension)
            .ToList();

        return types ?? new List<string>();
    }
}