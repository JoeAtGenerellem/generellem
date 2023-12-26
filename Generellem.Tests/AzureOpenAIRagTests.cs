using Azure;
using Azure.AI.OpenAI;

using Generellem.Document.DocumentTypes;
using Generellem.Llm;
using Generellem.Rag.AzureOpenAI;
using Generellem.Services;
using Generellem.Services.Azure;
using Generellem.Tests;

using Microsoft.Extensions.Configuration;

namespace Generellem.Rag.Tests;

public class AzureOpenAIRagTests
{
    Mock<IAzureSearchService> azSearchSvcMock = new();
    Mock<IConfiguration> configMock = new();
    Mock<IDocumentType> docTypeMock = new();
    Mock<LlmClientFactory> llmClientFactMock = new();
    Mock<OpenAIClient> openAIClientMock = new();
    Mock<Response<Embeddings>> embeddingsMock = new();

    IRag azureOpenAIRag;
    ReadOnlyMemory<float> embedding;

    public AzureOpenAIRagTests()
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
        List<EmbeddingItem> embeddingItems = new()
        {
            AzureOpenAIModelFactory.EmbeddingItem(embedding)
        };
        Embeddings embeddings = AzureOpenAIModelFactory.Embeddings(embeddingItems);
        openAIClientMock.Setup(client => client.GetEmbeddingsAsync(It.IsAny<EmbeddingsOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingsMock.Object);
        embeddingsMock.SetupGet(embed => embed.Value).Returns(embeddings);
        llmClientFactMock.Setup(llm => llm.CreateOpenAIClient()).Returns(openAIClientMock.Object);

        var chunks = new List<TextChunk>
        {
            new TextChunk { Content = "chunk1" },
            new TextChunk { Content = "chunk2" }
        };
        azSearchSvcMock
            .Setup(srchSvc => srchSvc.SearchAsync<TextChunk>(It.IsAny<ReadOnlyMemory<float>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);

        azureOpenAIRag = new AzureOpenAIRag(azSearchSvcMock.Object, configMock.Object, llmClientFactMock.Object);
    }

    [Fact]
    public async Task EmbedAsync_CallsGetTextAsync()
    {
        await azureOpenAIRag.EmbedAsync(Mock.Of<Stream>(), docTypeMock.Object, "file", CancellationToken.None);

        docTypeMock.Verify(doc => doc.GetTextAsync(It.IsAny<Stream>(), "file"), Times.Once());
    }

    [Fact]
    public async Task EmbedAsync_CallsGetEmbeddingsAsync()
    {
        await azureOpenAIRag.EmbedAsync(Mock.Of<Stream>(), docTypeMock.Object, "file", CancellationToken.None);

        openAIClientMock.Verify(
            client => client.GetEmbeddingsAsync(It.IsAny<EmbeddingsOptions>(), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task EmbedAsync_ReturnsEmbeddedTextChunks()
    {
        TextChunk expectedChunk = new()
        {
            Content = "Test document text",
            Embedding = TestEmbeddings.CreateEmbeddingArray(),
            FileRef = "file"
        };
        docTypeMock
            .Setup(doc => doc.GetTextAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync("Test document text");

        List<TextChunk> textChunks = await azureOpenAIRag.EmbedAsync(Mock.Of<Stream>(), docTypeMock.Object, "file", CancellationToken.None);

        TextChunk actualChunk = textChunks.First();
        Assert.Equal(expectedChunk.Content, actualChunk.Content);
        Assert.Equal(expectedChunk.FileRef, actualChunk.FileRef);
        float[] expectedEmbedding = expectedChunk.Embedding.ToArray();
        float[] actualEmbedding = actualChunk.Embedding.ToArray();
        Assert.Equal(expectedEmbedding.Length, actualEmbedding.Length);
        for (int i = 0; i < expectedEmbedding.Length; i++)
            Assert.Equal(expectedEmbedding[i], actualEmbedding[i]);
    }

    [Fact]
    public async Task IndexAsync_CallsCreateIndex()
    {
        List<TextChunk> chunks = new()
        {
            new()
            {
                Content = "Test document text",
                Embedding = TestEmbeddings.CreateEmbeddingArray(),
                FileRef = "file"
            }
        };

        await azureOpenAIRag.IndexAsync(chunks, CancellationToken.None);

        azSearchSvcMock.Verify(srchSvc => srchSvc.CreateIndexAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task IndexAsync_WithEmptyChunks_DoesNotCallCreateIndex()
    {
        List<TextChunk> chunks = new();

        await azureOpenAIRag.IndexAsync(chunks, CancellationToken.None);

        azSearchSvcMock.Verify(srchSvc => srchSvc.CreateIndexAsync(It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task IndexAsync_CallsUploadDocuments()
    {
        List<TextChunk> chunks = new()
        {
            new()
            {
                Content = "Test document text",
                Embedding = TestEmbeddings.CreateEmbeddingArray(),
                FileRef = "file"
            }
        };

        await azureOpenAIRag.IndexAsync(chunks, CancellationToken.None);

        azSearchSvcMock.Verify(srchSvc => 
            srchSvc.UploadDocumentsAsync(chunks, It.IsAny<CancellationToken>()), 
            Times.Once());
    }

    [Fact]
    public async Task IndexAsync_CallsUploadDocumentsWithCorrectChunks()
    {
        List<TextChunk> chunks = new()
        {
            new()
            {
                Content = "Test document text",
                Embedding = TestEmbeddings.CreateEmbeddingArray(),
                FileRef = "file"
            }
        };

        await azureOpenAIRag.IndexAsync(chunks, CancellationToken.None);

        azSearchSvcMock.Verify(searchSvc => 
            searchSvc.UploadDocumentsAsync(It.Is<List<TextChunk>>(c => c == chunks), It.IsAny<CancellationToken>()), 
            Times.Once());
    }

    [Fact]
    public async Task IndexAsync_WithEmptyChunks_DoesNotCallUploadDocuments()
    {
        List<TextChunk> chunks = new();

        await azureOpenAIRag.IndexAsync(chunks, CancellationToken.None);

        azSearchSvcMock.Verify(srchSvc =>
            srchSvc.UploadDocumentsAsync(chunks, It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact]
    public async Task SearchAsync_CallsGetEmbeddingsAsync()
    {
        await azureOpenAIRag.SearchAsync("text", CancellationToken.None);

        openAIClientMock.Verify(
            client => client.GetEmbeddingsAsync(It.IsAny<EmbeddingsOptions>(), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task SearchAsync_CallsSearchAsyncWithEmbedding()
    {
        await azureOpenAIRag.SearchAsync("text", CancellationToken.None);

        azSearchSvcMock.Verify(srchSvc => 
            srchSvc.SearchAsync<TextChunk>(embedding, It.IsAny<CancellationToken>()), 
            Times.Once());
    }

    [Fact]
    public async Task SearchAsync_ReturnsChunkContents()
    {
        const string ExpectedContent = "chunk1";

        var result = await azureOpenAIRag.SearchAsync("text", CancellationToken.None);

        Assert.Equal(ExpectedContent, result.First());
    }
}
