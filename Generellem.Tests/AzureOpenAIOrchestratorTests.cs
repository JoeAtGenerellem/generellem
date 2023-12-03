using Generellem.DataSource;
using Generellem.Document.DocumentTypes;
using Generellem.Llm;
using Generellem.Orchestrator;
using Generellem.Rag;

using Moq;

namespace Generellem.Tests;
public class AzureOpenAIOrchestratorTests
{
    readonly Mock<IDocumentSource> docSourceMock = new();
    readonly Mock<ILlm> llmMock = new();
    readonly Mock<IRag> ragMock = new();

    readonly AzureOpenAIOrchestrator orchestrator;

    public AzureOpenAIOrchestratorTests()
    {
        orchestrator = new AzureOpenAIOrchestrator(docSourceMock.Object, llmMock.Object, ragMock.Object);
    }

    [Fact]
    public async Task ProcessFilesAsync_CallsGetFiles()
    {
        await orchestrator.ProcessFilesAsync(CancellationToken.None);

        docSourceMock.Verify(x => x.GetFiles(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task ProcessFilesAsync_ProcessesSupportedDocument()
    {
        List<FileInfo> fileInfo = new()
        {
            new FileInfo("file.txt")
        };
        docSourceMock.Setup(x => x.GetFiles(It.IsAny<CancellationToken>())).Returns(fileInfo);

        await orchestrator.ProcessFilesAsync(CancellationToken.None);

        ragMock.Verify(
            x => x.EmbedAsync(It.IsAny<Stream>(), It.IsAny<IDocumentType>(), It.IsAny<string>(), CancellationToken.None), 
            Times.Once());
    }

    [Fact]
    public async Task ProcessFilesAsync_SkipsUnsupportedDocument()
    {
        List<FileInfo> fileInfo = new()
        {
            new FileInfo("file.xyz")
        };
        docSourceMock.Setup(x => x.GetFiles(It.IsAny<CancellationToken>())).Returns(fileInfo);

        await orchestrator.ProcessFilesAsync(CancellationToken.None);

        ragMock.Verify(
            x => x.EmbedAsync(It.IsAny<Stream>(), It.IsAny<IDocumentType>(), It.IsAny<string>(), CancellationToken.None),
            Times.Never);
    }
}
