using Generellem.Document.DocumentTypes;

namespace Generellem.DocumentSource;

public record DocumentInfo(string FileRef, Stream DocStream, IDocumentType DocType);
