using Azure;
using Azure.AI.OpenAI;

using Generellem.Document.DocumentTypes;
using Generellem.Security;
using Generellem.Services.Azure;

namespace Generellem.Rag.AzureOpenAI;

/// <summary>
/// Performs Retrieval-Augmented Generation (RAG) for Azure OpenAI
/// </summary>
public class AzureOpenAIRag : IRag
{
    readonly ISecretStore secretStore;
    readonly IAzureSearchService searchSvc;

    public AzureOpenAIRag(ISecretStore secretStore, IAzureSearchService searchSvc)
    {
        this.secretStore = secretStore;
        this.searchSvc = searchSvc;
    }

    /// <summary>
    /// Breaks text into chunks and adds an embedding to each chunk based on the text in that chunk
    /// </summary>
    /// <param name="documentStream"><see cref="Stream"/> to read data from a document</param>
    /// <param name="docType"><see cref="IDocumentType"/> for extracting text from document</param>
    /// <param name="fileRef">Reference to file. e.g. either a path, url, or some other indicator of where the file came from</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns>List of <see cref="TextChunk"/></returns>
    public async Task<List<TextChunk>> EmbedAsync(Stream documentStream, IDocumentType docType, string fileRef, CancellationToken cancellationToken)
    {
        string fullText = docType.GetText(documentStream, fileRef);

        List<TextChunk> chunks = TextProcessor.BreakIntoChunks(fullText, fileRef);

        (OpenAIClient client, string embeddingName) = CreateClient();

        foreach (TextChunk chunk in chunks)
        {
            Response<Embeddings> embeddings = await client.GetEmbeddingsAsync(new EmbeddingsOptions(embeddingName, new string[] { fullText }));

            chunk.Embedding = embeddings.Value.Data.First().Embedding;
        }

        return chunks;
    }

    /// <summary>
    /// Creates an Azure Search index (if it doesn't already exist), uploads document chunks, and indexes the chunks.
    /// </summary>
    /// <param name="chunks">Mulitple <see cref="TextChunk"/> instances for a document.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public async Task IndexAsync(List<TextChunk> chunks, CancellationToken cancellationToken)
    {
        await searchSvc.CreateIndexAsync();
        await searchSvc.UploadDocumentsAsync(chunks);
    }

    /// <summary>
    /// Performs Vector Search for chunks matching given text.
    /// </summary>
    /// <param name="text">Text for searching for matches</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns>List of text chunks matching query</returns>
    public async Task<List<string>> SearchAsync(string text, CancellationToken cancellationToken)
    {
        (OpenAIClient client, string embeddingName) = CreateClient();

        Response<Embeddings> embeddings = await client.GetEmbeddingsAsync(new EmbeddingsOptions(embeddingName, new string[] { text }));
        ReadOnlyMemory<float> embedding = embeddings.Value.Data.First().Embedding;
        List<TextChunk> chunks = await searchSvc.SearchAsync<TextChunk>(embedding);

        return 
            (from  chunk in chunks
             select chunk.Content)
            .ToList();
    }

    /// <summary>
    /// Builds an instance <see cref="OpenAIClient"/>
    /// </summary>
    /// <returns><see cref="OpenAIClient"/></returns>
    /// <exception cref="ArgumentNullException">Thrown if config values not found</exception>
    (OpenAIClient, string) CreateClient()
    {
        string? endpointName = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT_NAME");
        _ = endpointName ?? throw new ArgumentNullException(nameof(endpointName));

        string? embeddingName = Environment.GetEnvironmentVariable("OPENAI_EMBEDDING_NAME");
        _ = embeddingName ?? throw new ArgumentNullException(nameof(embeddingName));

        string? key = secretStore["OPENAI_API_KEY"];
        _ = key ?? throw new ArgumentNullException(nameof(key));

        OpenAIClient client = new OpenAIClient(new Uri(endpointName), new AzureKeyCredential(key));

        return (client, embeddingName);
    }
}
