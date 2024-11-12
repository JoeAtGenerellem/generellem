
namespace Generellem.DocumentSource;

/// <summary>
/// Location to get documents for ingestion
/// </summary>
public interface IDocumentSource
{
    /// <summary>
    /// Describes the document source.
    /// </summary>
    string Description { get; set; }

    /// <summary>
    /// Every source has a unique prefix to disambiguate file paths/names.
    /// </summary>
    string Reference { get; set; }

    /// <summary>
    /// Scans the document source for documents.
    /// </summary>
    /// <param name="cancelToken">Cancels the task.</param>
    /// <returns><see cref="IAsyncEnumerable{T}"/> of <see cref="DocumentInfo"/>.</returns>
    IAsyncEnumerable<DocumentInfo> GetDocumentsAsync(CancellationToken cancelToken);
}
