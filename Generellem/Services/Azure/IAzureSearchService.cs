using Generellem.Rag;

namespace Generellem.Services.Azure;

public interface IAzureSearchService
{
    Task CreateIndexAsync(CancellationToken cancelToken);
    Task<List<TResponse>> SearchAsync<TResponse>(ReadOnlyMemory<float> embedding, CancellationToken cancelToken);
    Task UploadDocumentsAsync(List<TextChunk> documents, CancellationToken cancelToken);
}