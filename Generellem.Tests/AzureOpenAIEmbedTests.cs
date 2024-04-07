using Azure;
using Azure.AI.OpenAI;

using Generellem.Document.DocumentTypes;
using Generellem.Embedding;
using Generellem.Embedding.AzureOpenAI;
using Generellem.Llm;
using Generellem.Services;
using Generellem.Services.Azure;
using Generellem.Tests;

using Microsoft.Extensions.Logging;

namespace Generellem.Rag.Tests;

public class AzureOpenAIEmbedTests
{
    readonly Mock<IAzureSearchService> azSearchSvcMock = new();
    readonly Mock<IDynamicConfiguration> configMock = new();
    readonly Mock<IDocumentType> docTypeMock = new();
    readonly Mock<ILogger<AzureOpenAIEmbedding>> logMock = new();
    readonly Mock<LlmClientFactory> llmClientFactMock = new();
    readonly Mock<OpenAIClient> openAIClientMock = new();
    readonly Mock<Response<Embeddings>> embeddingsMock = new();

    readonly IEmbedding azureOpenAIEmbedding;
    readonly ReadOnlyMemory<float> embedding;

    public AzureOpenAIEmbedTests()
    {
        docTypeMock
            .Setup(doc => doc.GetTextAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync("text content");

        configMock
            .Setup(config => config[GKeys.AzOpenAIEndpointName])
            .Returns("https://generellem");
        configMock
            .Setup(config => config[GKeys.AzOpenAIEmbeddingName])
            .Returns("generellem-embedding");
        configMock
            .Setup(config => config[GKeys.AzOpenAIApiKey])
            .Returns("generellem-key");

        embedding = new ReadOnlyMemory<float>(TestEmbeddings.CreateEmbeddingArray());
        List<EmbeddingItem> embeddingItems =
        [
            AzureOpenAIModelFactory.EmbeddingItem(embedding)
        ];
        Embeddings embeddings = AzureOpenAIModelFactory.Embeddings(embeddingItems);

        openAIClientMock.Setup(client => client.GetEmbeddingsAsync(It.IsAny<EmbeddingsOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingsMock.Object);

        embeddingsMock.SetupGet(embed => embed.Value).Returns(embeddings);

        llmClientFactMock.Setup(llm => llm.CreateOpenAIClient()).Returns(openAIClientMock.Object);

        List<TextChunk> chunks =
        [
            new()
            {
                ID = "id1",
                Content = "chunk1",
                DocumentReference = "documentReference1"
            },
            new() 
            {
                ID = "id2",
                Content = "chunk2",
                DocumentReference = "documentReference2"
            }
        ];
        azSearchSvcMock
            .Setup(srchSvc => srchSvc.DoesIndexExistAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        azSearchSvcMock
            .Setup(srchSvc => srchSvc.GetDocumentReferencesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);
        azSearchSvcMock
            .Setup(srchSvc => srchSvc.SearchAsync<TextChunk>(It.IsAny<ReadOnlyMemory<float>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);

        azureOpenAIEmbedding = new AzureOpenAIEmbedding(
            configMock.Object, llmClientFactMock.Object, logMock.Object);
    }

    [Fact]
    public async Task EmbedAsync_CallsGetEmbeddingsAsync()
    {
        TextProcessor.ChunkSize = 5000;
        TextProcessor.Overlap = 100;

        await azureOpenAIEmbedding.EmbedAsync("Test document text", docTypeMock.Object, "file", CancellationToken.None);

        openAIClientMock.Verify(
            client => client.GetEmbeddingsAsync(It.IsAny<EmbeddingsOptions>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce());
    }

    [Fact]
    public async Task EmbedAsync_WithNoOverlap_BreaksTextInto2Chunks()
    {
        int oldChunSize = TextProcessor.ChunkSize;
        int oldOverlap = TextProcessor.Overlap;
        TextProcessor.ChunkSize = 9;
        TextProcessor.Overlap = 0;

        await azureOpenAIEmbedding.EmbedAsync("Test document text", docTypeMock.Object, "file", CancellationToken.None);

        openAIClientMock.Verify(
            client => client.GetEmbeddingsAsync(It.IsAny<EmbeddingsOptions>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        TextProcessor.ChunkSize = oldChunSize;
        TextProcessor.Overlap = oldOverlap;
    }

    [Fact]
    public async Task EmbedAsync_WithOverlap_BreaksTextInto3Chunks()
    {
        int oldChunSize = TextProcessor.ChunkSize;
        int oldOverlap = TextProcessor.Overlap;
        TextProcessor.ChunkSize = 9;
        TextProcessor.Overlap = 2;

        await azureOpenAIEmbedding.EmbedAsync("Test document text", docTypeMock.Object, "file", CancellationToken.None);

        openAIClientMock.Verify(
            client => client.GetEmbeddingsAsync(It.IsAny<EmbeddingsOptions>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
        TextProcessor.ChunkSize = oldChunSize;
        TextProcessor.Overlap = oldOverlap;
    }

    [Fact]
    public async Task EmbedAsync_OverlapsChunks()
    {
        int oldChunSize = TextProcessor.ChunkSize;
        int oldOverlap = TextProcessor.Overlap;
        TextProcessor.ChunkSize = 11;
        TextProcessor.Overlap = 3;

        await azureOpenAIEmbedding.EmbedAsync("Test document text", docTypeMock.Object, "file", CancellationToken.None);

        openAIClientMock.Verify(
            client => client.GetEmbeddingsAsync(It.Is<EmbeddingsOptions>(eo => eo.Input[0] == "Test docume"), It.IsAny<CancellationToken>()),
            Times.Once);
        openAIClientMock.Verify(
            client => client.GetEmbeddingsAsync(It.Is<EmbeddingsOptions>(eo => eo.Input[0] == "ument text"), It.IsAny<CancellationToken>()),
            Times.Once);
        TextProcessor.ChunkSize = oldChunSize;
        TextProcessor.Overlap = oldOverlap;
    }

    [Fact]
    public async Task EmbedAsync_ReturnsEmbeddedTextChunks()
    {
        TextChunk expectedChunk = new()
        {
            Content = "Test document text",
            Embedding = TestEmbeddings.CreateEmbeddingArray(),
            DocumentReference = "file"
        };
        docTypeMock
            .Setup(doc => doc.GetTextAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync("Test document text");

        List<TextChunk> textChunks = await azureOpenAIEmbedding.EmbedAsync("Test document text", docTypeMock.Object, "file", CancellationToken.None);

        TextChunk actualChunk = textChunks.First();
        Assert.Equal(expectedChunk.Content, actualChunk.Content);
        Assert.Equal(expectedChunk.DocumentReference, actualChunk.DocumentReference);
        float[] expectedEmbedding = expectedChunk.Embedding.ToArray();
        float[] actualEmbedding = actualChunk.Embedding.ToArray();
        Assert.Equal(expectedEmbedding.Length, actualEmbedding.Length);
        for (int i = 0; i < expectedEmbedding.Length; i++)
            Assert.Equal(expectedEmbedding[i], actualEmbedding[i]);
    }

    [Fact]
    public async Task EmbedAsync_WithRequestFailedException_LogsAnError()
    {
        openAIClientMock
            .Setup(client => client.GetEmbeddingsAsync(It.IsAny<EmbeddingsOptions>(), It.IsAny<CancellationToken>()))
            .Throws(new RequestFailedException("Unauthorized"));

        await Assert.ThrowsAsync<RequestFailedException>(async () =>
            await azureOpenAIEmbedding.EmbedAsync("Test document text", docTypeMock.Object, "file", CancellationToken.None));

        logMock
            .Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
    }

}
