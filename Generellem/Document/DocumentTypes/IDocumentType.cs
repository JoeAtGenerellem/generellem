namespace Generellem.Document.DocumentTypes;

/// <summary>
/// A document that can be processed by Generellem
/// </summary>
public interface IDocumentType
{
    /// <summary>
    /// Indicates whether there's support for this document type.
    /// </summary>
    public bool CanProcess { get; set; }

    /// <summary>
    /// Which file extensions an implementation can support.
    /// </summary>
    List<string> SupportedExtensions { get; }

    /// <summary>
    /// Converts <see cref="Stream"/> into text.
    /// </summary>
    /// <param name="documentStream"><see cref="Stream"/> for accessing document data.</param>
    /// <param name="fileName">Sometimes we need the file name (more specifically - extension) to know how to process the file.</param>
    /// <returns>Full document text.</returns>
    Task<string> GetTextAsync(Stream documentStream, string fileName);
}
