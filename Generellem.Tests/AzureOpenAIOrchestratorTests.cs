using Generellem.DataSource;
using Generellem.Document.DocumentTypes;
using Generellem.Llm;
using Generellem.Llm.AzureOpenAI;
using Generellem.Orchestrator;
using Generellem.Rag;
using Generellem.Services;

using Microsoft.Extensions.Configuration;

using Moq;

namespace Generellem.Tests;

public class AzureOpenAIOrchestratorTests
{
    readonly Mock<IConfiguration> configMock = new();
    readonly Mock<IDocumentSource> docSourceMock = new();
    readonly Mock<ILlm> llmMock = new();
    readonly Mock<IRag> ragMock = new();

    readonly AzureOpenAIOrchestrator orchestrator;

    public AzureOpenAIOrchestratorTests()
    {
        ragMock
            .Setup(rag => rag.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new List<string> { "Search Result" }));
        llmMock
            .Setup(llm => llm.AskAsync<AzureOpenAIChatResponse>(It.IsAny<IChatRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new AzureOpenAIChatResponse()));

        orchestrator = new AzureOpenAIOrchestrator(configMock.Object, docSourceMock.Object, llmMock.Object, ragMock.Object);
    }

    [Fact]
    public async Task AskAsync_WithNullDeploymentName_ThrowsArgumentNullException()
    {
        configMock.SetupGet(config => config[GKeys.AzOpenAIDeploymentName]).Returns(value: null);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
          orchestrator.AskAsync("Hello", CancellationToken.None));
    }

    [Fact]
    public async Task AskAsync_WithEmptyDeploymentName_ThrowsArgumentNullException()
    {
        configMock.Setup(config => config[GKeys.AzOpenAIDeploymentName]).Returns(string.Empty);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            orchestrator.AskAsync("Hello", CancellationToken.None));
    }

    [Fact]
    public async Task AskAsync_CallsSearchAsync()
    {
        Mock<IConfigurationSection> configSection = new();

        configMock
            .Setup(config => config[GKeys.AzOpenAIDeploymentName])
            .Returns("generellem");

        await orchestrator.AskAsync("Hello", CancellationToken.None);

        ragMock.Verify(rag => rag.SearchAsync("Hello", CancellationToken.None), Times.Once());
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

        await orchestrator.AskAsync("Hello", CancellationToken.None);

        llmMock.Verify(llm =>
            llm.AskAsync<AzureOpenAIChatResponse>(
                It.IsAny<AzureOpenAIChatRequest>(),
                It.IsAny<CancellationToken>()));
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
        List<FileInfo> fileInfo = new()
        {
            new FileInfo("file.txt")
        };
        docSourceMock.Setup(docSrc => docSrc.GetFiles(It.IsAny<CancellationToken>())).Returns(fileInfo);

        await orchestrator.ProcessFilesAsync(CancellationToken.None);

        ragMock.Verify(
            rag => rag.EmbedAsync(It.IsAny<Stream>(), It.IsAny<IDocumentType>(), It.IsAny<string>(), CancellationToken.None), 
            Times.Once());
    }

    [Fact]
    public async Task ProcessFilesAsync_SkipsUnsupportedDocument()
    {
        List<FileInfo> fileInfo = new()
        {
            new FileInfo("file.xyz")
        };
        docSourceMock.Setup(docSrc => docSrc.GetFiles(It.IsAny<CancellationToken>())).Returns(fileInfo);

        await orchestrator.ProcessFilesAsync(CancellationToken.None);

        ragMock.Verify(
            rag => rag.EmbedAsync(It.IsAny<Stream>(), It.IsAny<IDocumentType>(), It.IsAny<string>(), CancellationToken.None),
            Times.Never);
    }
}
