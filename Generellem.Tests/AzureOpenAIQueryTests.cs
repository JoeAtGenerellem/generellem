using System.Text.Json;

using Azure.AI.OpenAI;

using Generellem.DocumentSource;
using Generellem.Embedding;
using Generellem.Llm;
using Generellem.Llm.AzureOpenAI;
using Generellem.Rag;
using Generellem.Services.Azure;

using Microsoft.Extensions.Logging;

namespace Generellem.Processors.Tests;

public class AzureOpenAIQueryTests
{
    readonly string DocSource = $"{Environment.MachineName}:{nameof(FileSystem)}";

    readonly Mock<IAzureSearchService> azSearchSvcMock = new();
    readonly Mock<IEmbedding> embedMock = new();
    readonly Mock<ILlm> llmMock = new();
    readonly Mock<ILlmClientFactory> llmFactMock = new();
    readonly Mock<ILogger<AzureOpenAIQuery>> logMock = new();
    readonly Mock<IRag> ragMock = new();

    readonly AzureOpenAIQuery azureQuery;
    readonly Queue<ChatMessage> chatHistory = new();

    readonly List<IDocumentSource> docSources = [];

    readonly AzureOpenAIChatRequest chatRequest = new();
    readonly AzureOpenAIChatResponse chatResponse = new();

    public AzureOpenAIQueryTests()
    {
        ragMock
            .Setup(rag => rag.BuildRequestAsync<AzureOpenAIChatRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Queue<ChatMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatRequest);
        llmMock
            .Setup(llm => llm.PromptAsync<AzureOpenAIChatResponse>(It.IsAny<IChatRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(chatResponse));

        azureQuery = new AzureOpenAIQuery(llmMock.Object, ragMock.Object);
    }

    [Fact]
    public async Task PromptAsync_CallsBuildRequestAsync()
    {
        await azureQuery.PromptAsync<AzureOpenAIChatRequest, AzureOpenAIChatResponse>(
            "Hello", chatHistory, CancellationToken.None);

        ragMock.Verify(rag => 
            rag.BuildRequestAsync<AzureOpenAIChatRequest>(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<Queue<ChatMessage>>(), 
                CancellationToken.None), 
            Times.Once());
    }

    [Fact]
    public async Task PromptAsync_CallsAskAsync()
    {
        await azureQuery.PromptAsync<AzureOpenAIChatRequest, AzureOpenAIChatResponse>(
            "Hello", chatHistory, CancellationToken.None);

        llmMock.Verify(llm => 
            llm.PromptAsync<AzureOpenAIChatResponse>(
                It.IsAny<IChatRequest>(), 
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task PromptAsync_ReturnsDetails()
    {
        GenerellemQueryDetails<AzureOpenAIChatRequest, AzureOpenAIChatResponse> queryDetails = 
            await azureQuery.PromptAsync<AzureOpenAIChatRequest, AzureOpenAIChatResponse>(
                "Hello", chatHistory, CancellationToken.None);

        Assert.Same(chatRequest, queryDetails?.Request);
        Assert.Same(chatResponse, queryDetails?.Response);
    }
}
