﻿using Generellem.Embedding;

namespace Generellem.Services;

public interface ISearchService
{
    Task CreateIndexAsync(CancellationToken cancelToken);
    Task DeleteDocumentReferencesAsync(List<string> documentReferencesToDelete, CancellationToken cancellationToken);
    Task<List<TextChunk>> GetDocumentReferencesAsync(string docSourcePrefix, CancellationToken cancellationToken);
    Task<List<TextChunk>> SearchAsync(ReadOnlyMemory<float> embedding, CancellationToken cancelToken);
    Task UploadDocumentsAsync(List<TextChunk> documents, CancellationToken cancelToken);
}