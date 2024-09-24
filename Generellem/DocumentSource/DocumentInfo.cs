using Generellem.Document.DocumentTypes;

namespace Generellem.DocumentSource;

/// <summary>
/// All of the info necessary to save a document in a vector DB.
/// </summary>
/// <param name="docSourcePrefix">Unique document source, such as <see cref="FileSystem"/> or <see cref="Website"/>.</param>
/// <param name="docStream"><see cref="System.IO.Stream"/> with document data.</param>
/// <param name="docType">Type of document in the <see cref="System.IO.Stream"/>.</param>
/// <param name="docPath">The full path/URL of the file within its document source.</param>
/// <param name="pathDescription">Description of the documents at the path in the document source.</param>
public class DocumentInfo(string? docSourcePrefix, Stream? docStream, IDocumentType? docType, string? docPath, string? pathDescription)
{
    /// <summary>
    /// <see cref="System.IO.Stream"/> with document data.
    /// </summary>
    public Stream? DocStream { get; set; } = docStream;

    /// <summary>
    /// Type of document in the <see cref="System.IO.Stream"/>.
    /// </summary>
    public IDocumentType? DocType { get; set; } = docType;

    /// <summary>
    /// Full path of the document within the document source.
    /// </summary>
    public string? DocPath { get; set; } = docPath;

    /// <summary>
    /// Description of the documents at the path in the document source.
    /// </summary>
    public string? PathDescription { get; set; } = pathDescription;

    /// <summary>
    /// Uniquely defines a document across different sources.
    /// </summary>
    public string DocumentReference { get; set; } = $"{docSourcePrefix}@{docPath}";
}
