using Azure.AI.OpenAI;

using Generellem.Document.DocumentTypes;
using Generellem.DocumentSource;
using Generellem.Rag;
using Generellem.Repository;
using Generellem.Services;

using Microsoft.Extensions.Logging;

namespace Generellem.Processors.Tests;

public class IngestionTests
{
    const string SpecDescription = "Test Spec Description";

    readonly string DocSource = $"{Environment.MachineName}:{nameof(FileSystem)}";

    readonly Mock<IDocumentHashRepository> docHashRepMock = new();
    readonly Mock<IDocumentSource> docSourceMock = new();
    readonly Mock<IDocumentSourceFactory> docSourceFactoryMock = new();
    readonly Mock<IDocumentType> docTypeMock = new();
    readonly Mock<ILogger<Ingestion>> logMock = new();
    readonly Mock<IRag> ragMock = new();

    readonly Ingestion ingestion;
    readonly Queue<ChatMessage> chatHistory = new();

    readonly List<IDocumentSource> docSources = [];

    public IngestionTests()
    {
        docSources.Add(docSourceMock.Object);

        docSourceFactoryMock
            .Setup(fact => fact.GetDocumentSources())
            .Returns(docSources);
        ragMock
            .Setup(rag => rag.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new List<string> { "Search Result" }));

        ingestion = new Ingestion(docHashRepMock.Object, docSourceFactoryMock.Object, logMock.Object, ragMock.Object);
    }

    void SetupGetDocumentsAsync(string filePath)
    {
        async IAsyncEnumerable<DocumentInfo> GetDocInfos()
        {
            yield return new DocumentInfo(DocSource, new MemoryStream(), new Text(), filePath, SpecDescription);
            await Task.CompletedTask;
        }
        docSourceMock.Setup(docSrc => docSrc.GetDocumentsAsync(It.IsAny<CancellationToken>())).Returns(GetDocInfos);
    }

    [Fact]
    public async Task ProcessFilesAsync_CallsGetDocumentsAsync()
    {
        SetupGetDocumentsAsync("TestDocs\\file.txt");

        await ingestion.IngestDocumentsAsync(new Progress<IngestionProgress>(), CancellationToken.None);

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
            yield return new DocumentInfo(DocSource, Mock.Of<MemoryStream>(), docTypeMock.Object, "file.txt", SpecDescription);
            await Task.CompletedTask;
        }
        docSourceMock.Setup(docSrc => docSrc.GetDocumentsAsync(It.IsAny<CancellationToken>())).Returns(GetDocInfos);

        await ingestion.IngestDocumentsAsync(new Progress<IngestionProgress>(), CancellationToken.None);

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
            yield return new DocumentInfo(DocSource, Mock.Of<MemoryStream>(), docTypeMock.Object, "file.txt", SpecDescription);
            await Task.CompletedTask;
        }
        docSourceMock.Setup(docSrc => docSrc.GetDocumentsAsync(It.IsAny<CancellationToken>())).Returns(GetDocInfos);

        await ingestion.IngestDocumentsAsync(new Progress<IngestionProgress>(), CancellationToken.None);

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

        await ingestion.IngestDocumentsAsync(new Progress<IngestionProgress>(), CancellationToken.None);

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
            yield return new DocumentInfo(DocSource, new MemoryStream(), new Unknown(), "file.xyz", SpecDescription);
            await Task.CompletedTask;
        }
        docSourceMock.Setup(docSrc => docSrc.GetDocumentsAsync(It.IsAny<CancellationToken>())).Returns(GetDocInfos);

        await ingestion.IngestDocumentsAsync(new Progress<IngestionProgress>(), CancellationToken.None);

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

        await ingestion.IngestDocumentsAsync(new Progress<IngestionProgress>(), CancellationToken.None);

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

        await ingestion.IngestDocumentsAsync(new Progress<IngestionProgress>(), CancellationToken.None);

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

        await ingestion.IngestDocumentsAsync(new Progress<IngestionProgress>(), CancellationToken.None);

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
