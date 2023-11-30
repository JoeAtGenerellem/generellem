using Generellem.Rag;

namespace Generellem.Services.Azure;

public interface IAzureSearchService
{
    Task CreateIndexAsync();
    Task<List<TResponse>> SearchAsync<TResponse>(ReadOnlyMemory<float> embedding);
    Task UploadDocumentsAsync(List<TextChunk> documents);
}