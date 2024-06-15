using Azure;
using Azure.AI.OpenAI;

using Generellem.Document.DocumentTypes;
using Generellem.Llm;
using Generellem.Processors;
using Generellem.Rag;
using Generellem.Services;
using Generellem.Services.Exceptions;

using Microsoft.Extensions.Logging;

using Polly;

namespace Generellem.Embedding.AzureOpenAI;

public class AzureOpenAIEmbedding(
    IDynamicConfiguration config, 
    LlmClientFactory llmClientFact, 
    ILogger<AzureOpenAIEmbedding> logger)
    : IEmbedding
{
    static IProgress<IngestionProgress>? currentProgress = null;

    public ResiliencePipeline Pipeline { get; set; } =
        new ResiliencePipelineBuilder()
            .AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is not GenerellemNeedsIngestionException)
            })
            .AddRetry(new()
            {
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(5),
                MaxRetryAttempts = 5,
                OnRetry = static args =>
                {
                    currentProgress?.Report(new("System busy, delaying a few seconds..."));
                    return default;
                }
            })
            .Build();

    /// <summary>
    /// Breaks text into chunks and adds an embedding to each chunk based on the text in that chunk.
    /// </summary>
    /// <param name="fullText">Full document text.</param>
    /// <param name="docType"><see cref="IDocumentType"/> for extracting text from document.</param>
    /// <param name="documentReference">Reference to file. e.g. either a path, url, or some other indicator of where the file came from.</param>
    /// <param name="progress">Reports progress.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns>List of <see cref="TextChunk"/></returns>
    public virtual async Task<List<TextChunk>> EmbedAsync(string fullText, IDocumentType docType, string documentReference, IProgress<IngestionProgress> progress, CancellationToken cancellationToken)
    {
        currentProgress = progress;

        List<TextChunk> chunks = TextProcessor.BreakIntoChunks(fullText, documentReference);

        int chunkCount = chunks.Count;
        int count = 0;
        string plural = chunkCount == 1 ? "" : "s";

        foreach (TextChunk chunk in chunks)
        {
            if (chunk.Content is null) continue;

            progress.Report(new($"Processing {++count} of {chunkCount} chunk{plural}"));

            try
            {
                OpenAIClient openAiClient = llmClientFact.CreateOpenAIClient();

                EmbeddingsOptions embeddingsOptions = GetEmbeddingOptions(chunk.Content);
                Response<Embeddings> embeddings = await Pipeline.ExecuteAsync<Response<Embeddings>>(
                    async token => await openAiClient.GetEmbeddingsAsync(embeddingsOptions, token),
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
    /// Embedding Options for Azure Search.
    /// </summary>
    /// <param name="text">Text string for calculating options.</param>
    /// <returns><see cref="EmbeddingsOptions"/></returns>
    public EmbeddingsOptions GetEmbeddingOptions(string text)
    {
        string? embeddingName = config[GKeys.AzOpenAIEmbeddingName];
        ArgumentException.ThrowIfNullOrWhiteSpace(embeddingName, nameof(embeddingName));

        EmbeddingsOptions embeddingsOptions = new(embeddingName, new string[] { text });

        return embeddingsOptions;
    }
}
