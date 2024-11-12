using Azure;
using Azure.AI.OpenAI;

using Generellem.Embedding;
using Generellem.Llm;
using Generellem.Llm.AzureOpenAI;
using Generellem.Rag.AzureOpenAI;
using Generellem.Services;
using Generellem.Tests;

using Microsoft.Extensions.Logging;

using OpenAI.Chat;

namespace Generellem.Rag.Tests;

public class AzureOpenAIRagTests
{
    readonly Mock<ISearchService> azSearchSvcMock = new();
    readonly Mock<IDynamicConfiguration> configMock = new();
    readonly Mock<IEmbedding> embedMock = new();
    readonly Mock<ILlm> llmMock = new();
    readonly Mock<ILogger<AzureOpenAIRag>> logMock = new();

    readonly Mock<AzureOpenAIClient> openAIClientMock = new();
    readonly Mock<LlmClientFactory> llmClientFactMock;

    readonly AzureOpenAIRag azureOpenAIRag;

    public AzureOpenAIRagTests()
    {
        configMock
            .Setup(config => config[GKeys.AzOpenAIEndpointName])
            .Returns("https://generellem");
        configMock
            .Setup(config => config[GKeys.AzOpenAIApiKey])
            .Returns("generellem-key");
        llmClientFactMock = new(configMock.Object);

        llmClientFactMock.Setup(llm => llm.CreateOpenAIClient()).Returns(openAIClientMock.Object);

        var embedding = new ReadOnlyMemory<float>(TestEmbeddings.CreateEmbeddingArray());
        embedMock
            .Setup(embed => embed.GetEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

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
            .Setup(srchSvc => srchSvc.SearchAsync(It.IsAny<ReadOnlyMemory<float>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);

        llmMock
            .Setup(llm => llm.PromptAsync<AzureOpenAIChatResponse>(It.IsAny<IChatRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Mock.Of<AzureOpenAIChatResponse>()));

        azureOpenAIRag = 
            new AzureOpenAIRag(
                azSearchSvcMock.Object, 
                configMock.Object, 
                embedMock.Object, 
                llmMock.Object, 
                logMock.Object);
    }

    [Fact]
    public async Task BuildRequestAsync_BuildsContextStringFromSearchResults()
    {
        var searchResults = new List<TextChunk> 
        { 
            new() { Content = "result1" },
            new() { Content = "result2" }
        };
        azSearchSvcMock
            .Setup(search => search.SearchAsync(It.IsAny<ReadOnlyMemory<float>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);
        configMock
            .Setup(config => config[GKeys.AzOpenAIDeploymentName])
            .Returns("generellem");

        AzureOpenAIChatRequest request = await azureOpenAIRag.BuildRequestAsync<AzureOpenAIChatRequest>(
            "Hello", new Queue<ChatMessage>(), CancellationToken.None);

        Assert.Contains(searchResults[0]?.Content ?? "", request.Text);
        Assert.Contains(searchResults[1]?.Content ?? "", request.Text);
    }

    [Fact]
    public async Task AskAsync_WithNullDeploymentName_ThrowsArgumentNullException()
    {
        Queue<ChatMessage> chatHistory = new();
        configMock.SetupGet(config => config[GKeys.AzOpenAIDeploymentName]).Returns(value: null);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
          azureOpenAIRag.BuildRequestAsync<AzureOpenAIChatRequest>(
            "What is Generellem?", chatHistory, CancellationToken.None));
    }

    [Fact]
    public async Task AskAsync_WithEmptyDeploymentName_ThrowsArgumentNullException()
    {
        Queue<ChatMessage> chatHistory = new();
        configMock.Setup(config => config[GKeys.AzOpenAIDeploymentName]).Returns(string.Empty);

        await Assert.ThrowsAsync<ArgumentException>(() =>
          azureOpenAIRag.BuildRequestAsync<AzureOpenAIChatRequest>(
            "What is Generellem?", chatHistory, CancellationToken.None));
    }

    [Fact]
    public async Task SearchAsync_CallsGetEmbeddingAsync()
    {
        await azureOpenAIRag.SearchAsync("text", CancellationToken.None);

        embedMock.Verify(
            client => client.GetEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task SearchAsync_CallsSearchAsyncWithEmbedding()
    {
        await azureOpenAIRag.SearchAsync("text", CancellationToken.None);

        azSearchSvcMock.Verify(srchSvc =>
            srchSvc.SearchAsync(It.IsAny<ReadOnlyMemory<float>>(), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task SearchAsync_ReturnsChunkContents()
    {
        const string ExpectedContent = "chunk1";

        List<TextChunk> chunks = await azureOpenAIRag.SearchAsync("text", CancellationToken.None);

        Assert.Equal(ExpectedContent, chunks.First().Content);
    }

    [Fact]
    public async Task SearchAsync_WithRequestFailedExceptionOnGetEmbeddings_LogsAnError()
    {
        embedMock
            .Setup(client => client.GetEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Throws(new RequestFailedException("Unauthorized"));

        await Assert.ThrowsAsync<RequestFailedException>(async () =>
            await azureOpenAIRag.SearchAsync("text", CancellationToken.None));

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

    [Fact]
    public async Task SearchAsync_WithRequestFailedExceptionOnAzSearch_LogsAnError()
    {
        azSearchSvcMock
            .Setup(svc => svc.SearchAsync(It.IsAny<ReadOnlyMemory<float>>(), It.IsAny<CancellationToken>()))
            .Throws(new RequestFailedException("Unauthorized"));

        await Assert.ThrowsAsync<RequestFailedException>(async () =>
            await azureOpenAIRag.SearchAsync("text", CancellationToken.None));

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
