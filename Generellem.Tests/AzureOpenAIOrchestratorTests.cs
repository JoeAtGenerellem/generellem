using Azure.AI.OpenAI;

using Generellem.Document.DocumentTypes;
using Generellem.DocumentSource;
using Generellem.Llm;
using Generellem.Llm.AzureOpenAI;
using Generellem.Rag;
using Generellem.Services;

using Microsoft.Extensions.Configuration;

namespace Generellem.Orchestrator.Tests;

public class AzureOpenAIOrchestratorTests
{
    readonly Mock<IConfiguration> configMock = new();
    readonly Mock<IDocumentSource> docSourceMock = new();
    readonly Mock<ILlm> llmMock = new();
    readonly Mock<IRag> ragMock = new();

    readonly AzureOpenAIOrchestrator orchestrator;
    readonly Queue<ChatMessage> chatHistory = new();

    public AzureOpenAIOrchestratorTests()
    {
        ragMock
            .Setup(rag => rag.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new List<string> { "Search Result" }));
        llmMock
            .Setup(llm => llm.AskAsync<AzureOpenAIChatResponse>(It.IsAny<IChatRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Mock.Of<AzureOpenAIChatResponse>()));

        orchestrator = new AzureOpenAIOrchestrator(configMock.Object, docSourceMock.Object, llmMock.Object, ragMock.Object);
    }

    [Fact]
    public async Task AskAsync_WithNullDeploymentName_ThrowsArgumentNullException()
    {
        configMock.SetupGet(config => config[GKeys.AzOpenAIDeploymentName]).Returns(value: null);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
          orchestrator.AskAsync("Hello", chatHistory, CancellationToken.None));
    }

    [Fact]
    public async Task AskAsync_WithEmptyDeploymentName_ThrowsArgumentNullException()
    {
        configMock.Setup(config => config[GKeys.AzOpenAIDeploymentName]).Returns(string.Empty);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            orchestrator.AskAsync("Hello", chatHistory, CancellationToken.None));
    }

    [Fact]
    public async Task AskAsync_CallsSearchAsync()
    {
        Mock<IConfigurationSection> configSection = new();

        configMock
            .Setup(config => config[GKeys.AzOpenAIDeploymentName])
            .Returns("generellem");

        await orchestrator.AskAsync("Hello", chatHistory, CancellationToken.None);

        ragMock.Verify(rag => rag.SearchAsync(It.IsAny<string>(), CancellationToken.None), Times.Once());
    }

    [Fact]
    public async Task AskAsync_CallsAskAsync()
    {
        Mock<IConfigurationSection> configSection = new();

        configMock
            .Setup(config => config[GKeys.AzOpenAIDeploymentName])
            .Returns("generellem");

        await orchestrator.AskAsync("Hello", chatHistory, CancellationToken.None);

        llmMock.Verify(llm => llm.AskAsync<AzureOpenAIChatResponse>(It.IsAny<IChatRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task AskAsync_BuildsContextStringFromSearchResults()
    {
        var searchResults = new List<string> { "result1", "result2" };
        ragMock
            .Setup(rag => rag.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);
        configMock
            .Setup(config => config[GKeys.AzOpenAIDeploymentName])
            .Returns("generellem");

        await orchestrator.AskAsync("Hello", chatHistory, CancellationToken.None);

        llmMock.Verify(llm =>
            llm.AskAsync<AzureOpenAIChatResponse>(
                It.IsAny<AzureOpenAIChatRequest>(),
                It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task AskAsync_PopulatesChatHistory()
    {
        const string ExpectedQuery = "What is Generellem?";
        var searchResults = new List<string> { "result1", "result2" };
        ragMock
            .Setup(rag => rag.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);
        configMock
            .Setup(config => config[GKeys.AzOpenAIDeploymentName])
            .Returns("generellem");

        await orchestrator.AskAsync(ExpectedQuery, chatHistory, CancellationToken.None);

        Assert.Single(chatHistory);
        ChatMessage chatMessage = chatHistory.Peek();
        Assert.Equal(ChatRole.User, chatMessage.Role);
        Assert.Equal(ExpectedQuery, chatMessage.Content);
    }

    [Fact]
    public async Task ProcessFilesAsync_CallsGetFiles()
    {
        await orchestrator.ProcessFilesAsync(CancellationToken.None);

        docSourceMock.Verify(docSrc => docSrc.GetFiles(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task ProcessFilesAsync_ProcessesSupportedDocument()
    {
        List<FileInfo> fileInfo =
        [
            new FileInfo("file.txt")
        ];
        docSourceMock.Setup(docSrc => docSrc.GetFiles(It.IsAny<CancellationToken>())).Returns(fileInfo);

        await orchestrator.ProcessFilesAsync(CancellationToken.None);

        ragMock.Verify(
            rag => rag.EmbedAsync(It.IsAny<Stream>(), It.IsAny<IDocumentType>(), It.IsAny<string>(), CancellationToken.None), 
            Times.Once());
    }

    [Fact]
    public async Task ProcessFilesAsync_SkipsUnsupportedDocument()
    {
        List<FileInfo> fileInfo =
        [
            new FileInfo("file.xyz")
        ];
        docSourceMock.Setup(docSrc => docSrc.GetFiles(It.IsAny<CancellationToken>())).Returns(fileInfo);

        await orchestrator.ProcessFilesAsync(CancellationToken.None);

        ragMock.Verify(
            rag => rag.EmbedAsync(It.IsAny<Stream>(), It.IsAny<IDocumentType>(), It.IsAny<string>(), CancellationToken.None),
            Times.Never);
    }
}
