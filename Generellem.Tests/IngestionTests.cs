using Azure;
using Azure.AI.OpenAI;

using Generellem.Document.DocumentTypes;
using Generellem.DocumentSource;
using Generellem.Embedding;
using Generellem.Llm;
using Generellem.Rag;
using Generellem.Rag.AzureOpenAI;
using Generellem.Repository;
using Generellem.Services;
using Generellem.Services.Azure;
using Generellem.Tests;

using Microsoft.Extensions.Logging;

namespace Generellem.Processors.Tests;

public class IngestionTests
{
    const string SpecDescription = "Test Spec Description";

    readonly string DocSource = $"{Environment.MachineName}:{nameof(FileSystem)}";

    readonly Mock<IAzureSearchService> azSearchSvcMock = new();
    readonly Mock<IDocumentHashRepository> docHashRepMock = new();
    readonly Mock<IDocumentSource> docSourceMock = new();
    readonly Mock<IDocumentSourceFactory> docSourceFactoryMock = new();
    readonly Mock<IDocumentType> docTypeMock = new();
    readonly Mock<IDynamicConfiguration> configMock = new();
    readonly Mock<IEmbedding> embedMock = new();
    readonly Mock<ILogger<Ingestion>> logMock = new();
    readonly Mock<IRag> ragMock = new();

    readonly Mock<OpenAIClient> openAIClientMock = new();
    readonly Mock<Response<Embeddings>> embeddingsMock = new();

    readonly Mock<LlmClientFactory> llmClientFactMock;

    readonly IGenerellemIngestion ingestion;

    readonly Queue<ChatRequestUserMessage> chatHistory = new();
    readonly List<IDocumentSource> docSources = [];

    readonly ReadOnlyMemory<float> embedding;

    public IngestionTests()
    {
        docSources.Add(docSourceMock.Object);

        docSourceFactoryMock
            .Setup(fact => fact.GetDocumentSources())
            .Returns(docSources);

        embedMock
            .Setup(e => e.GetEmbeddingOptions(It.IsAny<string>()))
            .Returns(new EmbeddingsOptions());

        embedding = new ReadOnlyMemory<float>(TestEmbeddings.CreateEmbeddingArray());
        List<EmbeddingItem> embeddingItems =
        [
            AzureOpenAIModelFactory.EmbeddingItem(embedding)
        ];
        Embeddings embeddings = AzureOpenAIModelFactory.Embeddings(embeddingItems);

        openAIClientMock
            .Setup(client => client.GetEmbeddingsAsync(It.IsAny<EmbeddingsOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingsMock.Object);
        embeddingsMock.SetupGet(embed => embed.Value).Returns(embeddings);

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

        embedMock
            .Setup(embed => embed.EmbedAsync(It.IsAny<string>(), It.IsAny<IDocumentType>(), It.IsAny<string>(), It.IsAny<IProgress<IngestionProgress>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);

        configMock
            .Setup(config => config[GKeys.AzOpenAIEndpointName])
            .Returns("https://generellem");
        configMock
            .Setup(config => config[GKeys.AzOpenAIApiKey])
            .Returns("generellem-key");
        llmClientFactMock = new(configMock.Object);

        llmClientFactMock.Setup(llm => llm.CreateOpenAIClient()).Returns(openAIClientMock.Object);

        ingestion = new Ingestion(
            azSearchSvcMock.Object, 
            docHashRepMock.Object, 
            docSourceFactoryMock.Object, 
            embedMock.Object,
            logMock.Object);
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
    public async Task IndexAsync_CallsCreateIndex()
    {
        List<TextChunk> chunks =
        [
            new()
            {
                Content = "Test document text",
                Embedding = TestEmbeddings.CreateEmbeddingArray(),
                DocumentReference = "file"
            }
        ];

        await ingestion.IndexAsync(chunks, CancellationToken.None);

        azSearchSvcMock.Verify(srchSvc => srchSvc.CreateIndexAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task IndexAsync_WithEmptyChunks_DoesNotCallCreateIndex()
    {
        List<TextChunk> chunks = [];

        await ingestion.IndexAsync(chunks, CancellationToken.None);

        azSearchSvcMock.Verify(srchSvc => srchSvc.CreateIndexAsync(It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task IndexAsync_CallsUploadDocuments()
    {
        List<TextChunk> chunks =
        [
            new()
            {
                Content = "Test document text",
                Embedding = TestEmbeddings.CreateEmbeddingArray(),
                DocumentReference = "file"
            }
        ];

        await ingestion.IndexAsync(chunks, CancellationToken.None);

        azSearchSvcMock.Verify(srchSvc =>
            srchSvc.UploadDocumentsAsync(chunks, It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task IndexAsync_CallsUploadDocumentsWithCorrectChunks()
    {
        List<TextChunk> chunks =
        [
            new()
            {
                Content = "Test document text",
                Embedding = TestEmbeddings.CreateEmbeddingArray(),
                DocumentReference = "file"
            }
        ];

        await ingestion.IndexAsync(chunks, CancellationToken.None);

        azSearchSvcMock.Verify(searchSvc =>
            searchSvc.UploadDocumentsAsync(It.Is<List<TextChunk>>(c => c == chunks), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task IndexAsync_WithEmptyChunks_DoesNotCallUploadDocuments()
    {
        List<TextChunk> chunks = [];

        await ingestion.IndexAsync(chunks, CancellationToken.None);

        azSearchSvcMock.Verify(srchSvc =>
            srchSvc.UploadDocumentsAsync(chunks, It.IsAny<CancellationToken>()),
            Times.Never());
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

        embedMock.Verify(
            embed => embed.EmbedAsync(It.IsAny<string>(), It.IsAny<IDocumentType>(), It.IsAny<string>(), It.IsAny<IProgress<IngestionProgress>>(), CancellationToken.None), 
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

        embedMock.Verify(
            embed => embed.EmbedAsync(It.IsAny<string>(), It.IsAny<IDocumentType>(), It.IsAny<string>(), It.IsAny<Progress<IngestionProgress>>(), CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public async Task ProcessFilesAsync_WithNewDocument_InsertsHash()
    {
        SetupGetDocumentsAsync("TestDocs\\file.txt");
        docHashRepMock
            .Setup(docHashRep => docHashRep.GetDocumentHashAsync(It.IsAny<string>()))
            .Returns(Task.FromResult<DocumentHash?>(null));

        await ingestion.IngestDocumentsAsync(new Progress<IngestionProgress>(), CancellationToken.None);

        docHashRepMock.Verify(
            docHashRep => docHashRep.InsertAsync(It.IsAny<DocumentHash>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessFilesAsync_WithChangedDocument_UpdatesHash()
    {
        SetupGetDocumentsAsync("TestDocs\\file.txt");
        docHashRepMock
            .Setup(docHashRep => docHashRep.GetDocumentHashAsync(It.IsAny<string>()))
            .Returns(Task.FromResult<DocumentHash?>(new DocumentHash{ DocumentReference = "someReference", Hash = Guid.NewGuid().ToString()}));

        await ingestion.IngestDocumentsAsync(new Progress<IngestionProgress>(), CancellationToken.None);

        docHashRepMock.Verify(
            docHashRep => docHashRep.UpdateAsync(It.IsAny<DocumentHash>(), It.IsAny<string>()),
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
            .Setup(docHashRep => docHashRep.GetDocumentHashAsync(It.IsAny<string>()))
            .Returns(Task.FromResult<DocumentHash?>(new DocumentHash { DocumentReference = "", Hash = SHA256BlankStringHash }));

        await ingestion.IngestDocumentsAsync(new Progress<IngestionProgress>(), CancellationToken.None);

        docHashRepMock.Verify(
            docHashRep => docHashRep.UpdateAsync(It.IsAny<DocumentHash>(), It.IsAny<string>()),
            Times.Never);
        docHashRepMock.Verify(
            docHashRep => docHashRep.InsertAsync(It.IsAny<DocumentHash>()),
            Times.Never);
        embedMock.Verify(
            embed => embed.EmbedAsync(It.IsAny<string>(), It.IsAny<IDocumentType>(), It.IsAny<string>(), It.IsAny<Progress<IngestionProgress>>(), CancellationToken.None),
            Times.Never);
    }
    [Fact]
    public async Task RemoveDeletedFilesAsync_WithNoDeletedFiles_DoesNotDeleteAnything()
    {
        string docSource = "Localhost:FileSystem";
        List<string> docSourceDocumentReferences = new List<string> { "documentReference1", "documentReference2" };

        await ingestion.RemoveDeletedFilesAsync(docSource, docSourceDocumentReferences, CancellationToken.None);

        azSearchSvcMock.Verify(
            srch => srch.DeleteDocumentReferencesAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RemoveDeletedFilesAsync_WithDeletedFiles_DeletesCorrectFiles()
    {
        string docSource = "Localhost:FileSystem";
        List<string> docSourceDocumentReferences = new List<string> { "file1" };

        await ingestion.RemoveDeletedFilesAsync(docSource, docSourceDocumentReferences, CancellationToken.None);

        azSearchSvcMock.Verify(
            srch => srch.DeleteDocumentReferencesAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
