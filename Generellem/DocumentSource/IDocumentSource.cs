namespace Generellem.DocumentSource;

/// <summary>
/// Location to get documents for ingestion
/// </summary>
public interface IDocumentSource
{
    /// <summary>
    /// Every source has a unique prefix to disambiguate file paths/names.
    /// </summary>
    string Prefix { get; init; }

    /// <summary>
    /// Scans the document source for documents and returns an <see cref="IAsyncEnumerable{T}"/> of <see cref="DocumentInfo"/>.
    /// </summary>
    /// <param name="cancelToken"></param>
    /// <returns></returns>
    IAsyncEnumerable<DocumentInfo> GetDocumentsAsync(CancellationToken cancelToken);
}
