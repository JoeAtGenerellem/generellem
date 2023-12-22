namespace Generellem.DocumentSource;

public interface IDocumentSource
{
    IAsyncEnumerable<DocumentInfo> GetDocumentsAsync(CancellationToken cancelToken);
}
