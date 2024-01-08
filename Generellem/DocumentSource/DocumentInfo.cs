using Generellem.Document.DocumentTypes;

namespace Generellem.DocumentSource;

/// <summary>
/// All of the info necessary to save a document in a vector DB.
/// </summary>
/// <param name="docSourcePrefix">Unique document source, such as <see cref="FileSystem"/> or <see cref="Website"/>.</param>
/// <param name="docStream"><see cref="Stream"/> with document data.</param>
/// <param name="docType">Type of document in <see cref="docStream"/>.</param>
/// <param name="filePath">The full path/URL of the file within it's document source.</param>
public class DocumentInfo(string? docSourcePrefix, Stream? docStream, IDocumentType? docType, string? filePath)
{
    /// <summary>
    /// <see cref="Stream"/> with document data.
    /// </summary>
    public Stream? DocStream { get; set; } = docStream;

    /// <summary>
    /// Type of document in <see cref="docStream"/>.
    /// </summary>
    public IDocumentType? DocType { get; set; } = docType;

    /// <summary>
    /// Full path of the file within the document source.
    /// </summary>
    public string? FilePath { get; set; } = filePath;

    /// <summary>
    /// Uniquely defines a file accross document sources.
    /// </summary>
    public string FileRef { get; set; } = $"{docSourcePrefix}@{filePath}";
}
