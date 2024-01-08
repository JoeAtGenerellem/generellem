using Generellem.Rag;

namespace Generellem.Services.Azure;

public interface IAzureSearchService
{
    Task CreateIndexAsync(CancellationToken cancelToken);
    Task DeleteFileRefsAsync(List<string> fileRefsToDelete, CancellationToken cancellationToken);
    Task<bool> DoesIndexExistAsync(CancellationToken cancellationToken);
    Task<List<TextChunk>> GetFileRefsAsync(string docSourcePrefix, CancellationToken cancellationToken);
    Task<List<TResponse>> SearchAsync<TResponse>(ReadOnlyMemory<float> embedding, CancellationToken cancelToken);
    Task UploadDocumentsAsync(List<TextChunk> documents, CancellationToken cancelToken);
}