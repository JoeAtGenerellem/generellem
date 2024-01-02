using Azure;
using Azure.AI.OpenAI;

using Generellem.Document.DocumentTypes;
using Generellem.Llm;
using Generellem.Services;
using Generellem.Services.Azure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Polly;
using Polly.Retry;

namespace Generellem.Rag.AzureOpenAI;

/// <summary>
/// Performs Retrieval-Augmented Generation (RAG) for Azure OpenAI
/// </summary>
public class AzureOpenAIRag(
    IAzureSearchService azSearchSvc, 
    IConfiguration config, 
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
    /// Breaks text into chunks and adds an embedding to each chunk based on the text in that chunk
    /// </summary>
    /// <param name="fullText">Full document text</param>
    /// <param name="docType"><see cref="IDocumentType"/> for extracting text from document</param>
    /// <param name="fileRef">Reference to file. e.g. either a path, url, or some other indicator of where the file came from</param>
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

        await azSearchSvc.CreateIndexAsync(cancellationToken);
        await azSearchSvc.UploadDocumentsAsync(chunks, cancellationToken);
    }

    /// <summary>
    /// Performs Vector Search for chunks matching given text.
    /// </summary>
    /// <param name="text">Text for searching for matches</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns>List of text chunks matching query</returns>
    public virtual async Task<List<string>> SearchAsync(string text, CancellationToken cancellationToken)
    {
        EmbeddingsOptions embeddingsOptions = GetEmbeddingOptions(text);

        try
        {
            Response<Embeddings> embeddings = await pipeline.ExecuteAsync<Response<Embeddings>>(
                async token => await openAIClient.GetEmbeddingsAsync(embeddingsOptions, token),
                cancellationToken);

            ReadOnlyMemory<float> embedding = embeddings.Value.Data[0].Embedding;
            List<TextChunk> chunks = await azSearchSvc.SearchAsync<TextChunk>(embedding, cancellationToken);

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
