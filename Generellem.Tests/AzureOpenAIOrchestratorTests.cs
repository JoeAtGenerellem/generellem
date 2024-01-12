using Azure.AI.OpenAI;

using Generellem.Document.DocumentTypes;
using Generellem.DocumentSource;
using Generellem.Llm;
using Generellem.Llm.AzureOpenAI;
using Generellem.Rag;
using Generellem.Repository;
using Generellem.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Generellem.Orchestrator.Tests;

public class AzureOpenAIOrchestratorTests
{
    readonly string DocSource = $"{Environment.MachineName}:{nameof(FileSystem)}";

    readonly Mock<IConfiguration> configMock = new();
    readonly Mock<IDocumentHashRepository> docHashRepMock = new();
    readonly Mock<IDocumentSource> docSourceMock = new();
    readonly Mock<IDocumentSourceFactory> docSourceFactoryMock = new();
    readonly Mock<IDocumentType> docTypeMock = new();
    readonly Mock<ILlm> llmMock = new();
    readonly Mock<ILogger<AzureOpenAIOrchestrator>> logMock = new();
    readonly Mock<IRag> ragMock = new();

    readonly AzureOpenAIOrchestrator orchestrator;
    readonly Queue<ChatMessage> chatHistory = new();

    readonly List<IDocumentSource> docSources = [];

    public AzureOpenAIOrchestratorTests()
    {
        docSources.Add(docSourceMock.Object);

        docSourceFactoryMock
            .Setup(fact => fact.GetDocumentSources())
            .Returns(docSources);
        ragMock
            .Setup(rag => rag.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new List<string> { "Search Result" }));
        llmMock
            .Setup(llm => llm.AskAsync<AzureOpenAIChatResponse>(It.IsAny<IChatRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Mock.Of<AzureOpenAIChatResponse>()));

        orchestrator = new AzureOpenAIOrchestrator(
            configMock.Object, docHashRepMock.Object, docSourceFactoryMock.Object, llmMock.Object, logMock.Object, ragMock.Object);
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

    void SetupGetDocumentsAsync(string filePath)
    {
        async IAsyncEnumerable<DocumentInfo> GetDocInfos()
        {
            yield return new DocumentInfo(DocSource, new MemoryStream(), new Text(), filePath);
            await Task.CompletedTask;
        }
        docSourceMock.Setup(docSrc => docSrc.GetDocumentsAsync(It.IsAny<CancellationToken>())).Returns(GetDocInfos);
    }

    [Fact]
    public async Task ProcessFilesAsync_CallsGetDocumentsAsync()
    {
        SetupGetDocumentsAsync("TestDocs\\file.txt");

        await orchestrator.ProcessFilesAsync(CancellationToken.None);

        docSourceMock.Verify(docSrc => docSrc.GetDocumentsAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task ProcessFilesAsync_CallsGetTextAsync()
    {
        SetupGetDocumentsAsync("TestDocs\\file.txt");
        docTypeMock
            .Setup(docType => docType.GetTextAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync("Sample Text");
        async IAsyncEnumerable<DocumentInfo> GetDocInfos()
        {
            yield return new DocumentInfo(DocSource, Mock.Of<MemoryStream>(), docTypeMock.Object, "file.txt");
            await Task.CompletedTask;
        }
        docSourceMock.Setup(docSrc => docSrc.GetDocumentsAsync(It.IsAny<CancellationToken>())).Returns(GetDocInfos);

        await orchestrator.ProcessFilesAsync(CancellationToken.None);

        docTypeMock.Verify(
            docType => docType.GetTextAsync(It.IsAny<Stream>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessFilesAsync_GetTextAsyncThrows_LogsWarning()
    {
        SetupGetDocumentsAsync("TestDocs\\file.txt");
        docTypeMock
            .Setup(docType => docType.GetTextAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception());
        async IAsyncEnumerable<DocumentInfo> GetDocInfos()
        {
            yield return new DocumentInfo(DocSource, Mock.Of<MemoryStream>(), docTypeMock.Object, "file.txt");
            await Task.CompletedTask;
        }
        docSourceMock.Setup(docSrc => docSrc.GetDocumentsAsync(It.IsAny<CancellationToken>())).Returns(GetDocInfos);

        await orchestrator.ProcessFilesAsync(CancellationToken.None);

        logMock
            .Verify(
                l => l.Log(
                    LogLevel.Warning,
                    GenerellemLogEvents.DocumentError,
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
    }

    [Fact]
    public async Task ProcessFilesAsync_ProcessesSupportedDocument()
    {
        SetupGetDocumentsAsync("TestDocs\\file.txt");

        await orchestrator.ProcessFilesAsync(CancellationToken.None);

        ragMock.Verify(
            rag => rag.EmbedAsync(It.IsAny<string>(), It.IsAny<IDocumentType>(), It.IsAny<string>(), CancellationToken.None), 
            Times.Once());
    }

    [Fact]
    public async Task ProcessFilesAsync_SkipsUnsupportedDocument()
    {
        SetupGetDocumentsAsync("TestDocs\\file.xyz");
        async IAsyncEnumerable<DocumentInfo> GetDocInfos()
        {
            yield return new DocumentInfo(DocSource, new MemoryStream(), new Unknown(), "file.xyz");
            await Task.CompletedTask;
        }
        docSourceMock.Setup(docSrc => docSrc.GetDocumentsAsync(It.IsAny<CancellationToken>())).Returns(GetDocInfos);

        await orchestrator.ProcessFilesAsync(CancellationToken.None);

        ragMock.Verify(
            rag => rag.EmbedAsync(It.IsAny<string>(), It.IsAny<IDocumentType>(), It.IsAny<string>(), CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public async Task ProcessFilesAsync_WithNewDocument_InsertsHash()
    {
        SetupGetDocumentsAsync("TestDocs\\file.txt");
        docHashRepMock
            .Setup(docHashRep => docHashRep.GetDocumentHash(It.IsAny<string>()))
            .Returns((DocumentHash?)null);

        await orchestrator.ProcessFilesAsync(CancellationToken.None);

        docHashRepMock.Verify(
            docHashRep => docHashRep.Insert(It.IsAny<DocumentHash>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessFilesAsync_WithChangedDocument_UpdatesHash()
    {
        SetupGetDocumentsAsync("TestDocs\\file.txt");
        docHashRepMock
            .Setup(docHashRep => docHashRep.GetDocumentHash(It.IsAny<string>()))
            .Returns(new DocumentHash { DocumentReference = "", Hash = Guid.NewGuid().ToString() });

        await orchestrator.ProcessFilesAsync(CancellationToken.None);

        docHashRepMock.Verify(
            docHashRep => docHashRep.Update(It.IsAny<DocumentHash>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessFilesAsync_WithUnchangedDocument_DoesNotProcess()
    {
        // DocumentInfo from SetupGetDocumentsAsync has an empty MemoryStream
        // that returns an empty string that hashes to SHA256BlankStringHash.
        const string SHA256BlankStringHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        SetupGetDocumentsAsync("TestDocs\\file.txt");
        docHashRepMock
            .Setup(docHashRep => docHashRep.GetDocumentHash(It.IsAny<string>()))
            .Returns(new DocumentHash { DocumentReference = "", Hash = SHA256BlankStringHash });

        await orchestrator.ProcessFilesAsync(CancellationToken.None);

        docHashRepMock.Verify(
            docHashRep => docHashRep.Update(It.IsAny<DocumentHash>(), It.IsAny<string>()),
            Times.Never);
        docHashRepMock.Verify(
            docHashRep => docHashRep.Insert(It.IsAny<DocumentHash>()),
            Times.Never);
        ragMock.Verify(
            rag => rag.EmbedAsync(It.IsAny<string>(), It.IsAny<IDocumentType>(), It.IsAny<string>(), CancellationToken.None),
            Times.Never);
    }
}
