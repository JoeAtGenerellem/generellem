using Azure;
using Azure.AI.OpenAI;

using Generellem.Document.DocumentTypes;
using Generellem.Llm;
using Generellem.Repository;
using Generellem.Services;
using Generellem.Services.Azure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Polly;
using Polly.Retry;

namespace Generellem.Rag.AzureOpenAI;

/// <summary>
/// Performs Retrieval-Augmented Generation (RAG) for Azure OpenAI.
/// </summary>
public class AzureOpenAIRag(
    IAzureSearchService azSearchSvc, 
    IConfiguration config, 
    IDocumentHashRepository docHashRep,
    LlmClientFactory llmClientFact, 
    ILogger<AzureOpenAIRag> logger) 
    : IRag
{
    readonly IAzureSearchService azSearchSvc = azSearchSvc;
    readonly IConfiguration config = config;
    readonly ILogger<AzureOpenAIRag> logger = logger;

    readonly OpenAIClient openAIClient = llmClientFact.CreateOpenAIClient();

    readonly ResiliencePipeline pipeline = 
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions())
            .AddTimeout(TimeSpan.FromSeconds(3))
            .Build();

    /// <summary>
    /// Breaks text into chunks and adds an embedding to each chunk based on the text in that chunk.
    /// </summary>
    /// <param name="fullText">Full document text.</param>
    /// <param name="docType"><see cref="IDocumentType"/> for extracting text from document.</param>
    /// <param name="fileRef">Reference to file. e.g. either a path, url, or some other indicator of where the file came from.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns>List of <see cref="TextChunk"/></returns>
    public virtual async Task<List<TextChunk>> EmbedAsync(string fullText, IDocumentType docType, string fileRef, CancellationToken cancellationToken)
    {
        List<TextChunk> chunks = TextProcessor.BreakIntoChunks(fullText, fileRef);
        EmbeddingsOptions embeddingsOptions = GetEmbeddingOptions(fullText);

        foreach (TextChunk chunk in chunks)
        {
            try
            {
                Response<Embeddings> embeddings = await pipeline.ExecuteAsync<Response<Embeddings>>(
                    async token => await openAIClient.GetEmbeddingsAsync(embeddingsOptions, token),
                    cancellationToken);

                chunk.Embedding = embeddings.Value.Data[0].Embedding;
            }
            catch (RequestFailedException rfEx)
            {
                logger.LogError(GenerellemLogEvents.AuthorizationFailure, rfEx, "Please check credentials and exception details for more info.");
                throw;
            }
        }

        return chunks;
    }

    /// <summary>
    /// Creates an Azure Search index (if it doesn't already exist), uploads document chunks, and indexes the chunks.
    /// </summary>
    /// <param name="chunks">Mulitple <see cref="TextChunk"/> instances for a document.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public virtual async Task IndexAsync(List<TextChunk> chunks, CancellationToken cancellationToken)
    {
        if (chunks.Count is 0)
            return;

        await pipeline.ExecuteAsync(
            async token => await azSearchSvc.CreateIndexAsync(token),
            cancellationToken);
        await pipeline.ExecuteAsync(
            async token => await azSearchSvc.UploadDocumentsAsync(chunks, token),
            cancellationToken);
    }

    /// <summary>
    /// Deletes file refs from the index and local DB that aren't in the fileRefs argument.
    /// </summary>
    /// <remarks>
    /// The assumption here is that for a given document source, we've identified
    /// all of the files that we can process. However, if there's a file in the
    /// index and not in the document source, the file must have been deleted.
    /// </remarks>
    /// <param name="docSource">Filters the fileRefs that can be deleted.</param>
    /// <param name="docSourceFileRefs">Existing fileRefs.</param>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    public async Task RemoveDeletedFilesAsync(string docSource, List<string> docSourceFileRefs, CancellationToken cancellationToken)
    {
        bool doesIndexExist = await azSearchSvc.DoesIndexExistAsync(cancellationToken);
        if (!doesIndexExist)
            return;

        List<TextChunk> chunks = await azSearchSvc.GetFileRefsAsync(docSource, cancellationToken);

        List<string> chunkIdsToDelete = new();
        List<string> chunkFileRefsToDelete = new();

        foreach (TextChunk chunk in chunks)
        {
            if (chunk.FileRef is null || docSourceFileRefs.Contains(chunk.FileRef))
                continue;

            if (chunk?.ID is string chunkID)
                chunkIdsToDelete.Add(chunkID);
            if (chunk?.FileRef is string chunkFileRef)
                chunkFileRefsToDelete.Add(chunkFileRef);
        }

        if (chunkIdsToDelete.Count != 0)
        {
            await pipeline.ExecuteAsync(
                async token => await azSearchSvc.DeleteFileRefsAsync(chunkIdsToDelete, token),
                cancellationToken);
            docHashRep.Delete(chunkFileRefsToDelete);
        }
    }

    /// <summary>
    /// Performs Vector Search for chunks matching given text.
    /// </summary>
    /// <param name="text">Text for searching for matches.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns>List of text chunks matching query.</returns>
    public virtual async Task<List<string>> SearchAsync(string text, CancellationToken cancellationToken)
    {
        EmbeddingsOptions embeddingsOptions = GetEmbeddingOptions(text);

        try
        {
            Response<Embeddings> embeddings = await pipeline.ExecuteAsync<Response<Embeddings>>(
                async token => await openAIClient.GetEmbeddingsAsync(embeddingsOptions, token),
                cancellationToken);

            ReadOnlyMemory<float> embedding = embeddings.Value.Data[0].Embedding;
            List<TextChunk> chunks = await pipeline.ExecuteAsync(
                async token => await azSearchSvc.SearchAsync<TextChunk>(embedding, token),
                cancellationToken);

            return
                (from chunk in chunks
                 select chunk.Content)
                .ToList();
        }
        catch (RequestFailedException rfEx)
        {
            logger.LogError(GenerellemLogEvents.AuthorizationFailure, rfEx, "Please check credentials and exception details for more info.");
            throw;
        }
    }

    EmbeddingsOptions GetEmbeddingOptions(string text)
    {
        string? embeddingName = config[GKeys.AzOpenAIEmbeddingName];
        ArgumentException.ThrowIfNullOrWhiteSpace(embeddingName, nameof(embeddingName));

        EmbeddingsOptions embeddingsOptions = new(embeddingName, new string[] { text });

        return embeddingsOptions;
    }
}
