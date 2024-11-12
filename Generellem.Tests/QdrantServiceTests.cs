using Azure;

using Generellem.Embedding;
using Generellem.Services.Azure;
using Generellem.Services.Exceptions;

using Microsoft.Extensions.Logging;

using Polly;
using Polly.Retry;

using Qdrant.Client;
using Qdrant.Client.Grpc;

using System.Net;

namespace Generellem.Services.Tests;

// TODO: QdrantClient is non-overrideable

//public class QdrantServiceTests
//{
//    readonly Mock<IDynamicConfiguration> configMock = new();
//    readonly Mock<ILogger<QdrantService>> loggerMock = new();
//    readonly QdrantService qdrantService;
//    readonly Mock<QdrantClient> qdrantClientMock = new();
//    readonly ResiliencePipeline pipeline;

//    public QdrantServiceTests()
//    {
//        configMock.Setup(config => config[GKeys.AzSearchServiceAdminApiKey]).Returns("adminApiKey");
//        configMock.Setup(config => config[GKeys.AzSearchServiceEndpoint]).Returns("https://searchservice.endpoint");
//        configMock.Setup(config => config[GKeys.AzSearchServiceIndex]).Returns("searchIndex");

//        pipeline = new ResiliencePipelineBuilder()
//            .AddRetry(new RetryStrategyOptions())
//            .AddTimeout(TimeSpan.FromSeconds(3))
//            .Build();

//        qdrantService = new QdrantService(configMock.Object, loggerMock.Object);
//    }

//    [Fact]
//    public async Task CreateIndexAsync_CreatesIndexSuccessfully()
//    {
//        qdrantClientMock
//            .Setup(client => 
//                client.CreateCollectionAsync(
//                    It.IsAny<string>(), 
//                    It.IsAny<VectorParams>(),
//                    It.IsAny<uint>(),
//                    It.IsAny<uint>(),
//                    It.IsAny<uint>(),
//                    It.IsAny<bool>(),
//                    It.IsAny<HnswConfigDiff>(),
//                    It.IsAny<OptimizersConfigDiff>(),
//                    It.IsAny<WalConfigDiff>(),
//                    It.IsAny<QuantizationConfig>(),
//                    It.IsAny<string>(),
//                    It.IsAny<ShardingMethod>(),
//                    It.IsAny<SparseVectorConfig>(),
//                    It.IsAny<TimeSpan>(),
//                    It.IsAny<CancellationToken>()))
//            .Returns(Task.CompletedTask);

//        await qdrantService.CreateIndexAsync(CancellationToken.None);

//        qdrantClientMock.Verify(client => 
//            client.CreateCollectionAsync(
//                "my_collection", 
//                It.Is<VectorParams>(v => true), 
//                It.IsAny<uint>(), 
//                It.IsAny<uint>(), 
//                It.IsAny<uint>(), 
//                It.IsAny<bool>(), 
//                It.IsAny<HnswConfigDiff>(), 
//                It.IsAny<OptimizersConfigDiff>(), 
//                It.IsAny<WalConfigDiff>(), 
//                It.IsAny<QuantizationConfig>(), 
//                It.IsAny<string>(), 
//                It.IsAny<ShardingMethod>(), 
//                It.IsAny<SparseVectorConfig>(), 
//                It.IsAny<TimeSpan>(), 
//                It.IsAny<CancellationToken>()), 
//            Times.Once);
//    }

//    [Fact]
//    public async Task CreateIndexAsync_ThrowsRequestFailedException_LogsError()
//    {
//        qdrantClientMock
//            .Setup(client => 
//                client.CreateCollectionAsync(
//                    It.IsAny<string>(), 
//                    It.IsAny<VectorParams>(), 
//                    It.IsAny<uint>(), 
//                    It.IsAny<uint>(), 
//                    It.IsAny<uint>(), 
//                    It.IsAny<bool>(), 
//                    It.IsAny<HnswConfigDiff>(), 
//                    It.IsAny<OptimizersConfigDiff>(), 
//                    It.IsAny<WalConfigDiff>(), 
//                    It.IsAny<QuantizationConfig>(), 
//                    It.IsAny<string>(), 
//                    It.IsAny<ShardingMethod>(), 
//                    It.IsAny<SparseVectorConfig>(), 
//                    It.IsAny<TimeSpan>(), 
//                    It.IsAny<CancellationToken>()))
//            .ThrowsAsync(new RequestFailedException("Unauthorized"));

//        await Assert.ThrowsAsync<RequestFailedException>(() => qdrantService.CreateIndexAsync(CancellationToken.None));

//        loggerMock.Verify(
//            l => l.Log(
//                LogLevel.Error,
//                It.IsAny<EventId>(),
//                It.IsAny<It.IsAnyType>(),
//                It.IsAny<Exception>(),
//                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
//            Times.Once);
//    }

//    [Fact]
//    public async Task DoesIndexExistAsync_ReturnsTrue_WhenIndexExists()
//    {
//        qdrantClientMock
//            .Setup(client => client.CollectionExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(true);

//        bool result = await qdrantService.DoesIndexExistAsync(CancellationToken.None);

//        Assert.True(result);
//    }

//    [Fact]
//    public async Task DoesIndexExistAsync_ReturnsFalse_WhenIndexDoesNotExist()
//    {
//        qdrantClientMock
//            .Setup(client => client.CollectionExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
//            .ThrowsAsync(new RequestFailedException((int)HttpStatusCode.NotFound, "Not Found"));

//        bool result = await qdrantService.DoesIndexExistAsync(CancellationToken.None);

//        Assert.False(result);
//    }

//    [Fact]
//    public async Task DeleteDocumentReferencesAsync_DeletesDocumentsSuccessfully()
//    {
//        List<string> idsToDelete = new() { "1", "2", "3" };
//        qdrantClientMock
//            .Setup(client => 
//                client.DeleteAsync(
//                    It.IsAny<string>(), 
//                    It.IsAny<IReadOnlyList<ulong>>(),
//                    It.IsAny<bool>(),
//                    It.IsAny<WriteOrderingType>(),
//                    It.IsAny<ShardKeySelector>(),
//                    It.IsAny<CancellationToken>()))
//            .ReturnsAsync(new UpdateResult());

//        await qdrantService.DeleteDocumentReferencesAsync(idsToDelete, CancellationToken.None);

//        qdrantClientMock.Verify(client => 
//            client.DeleteAsync(
//                "searchIndex", 
//                It.IsAny<IReadOnlyList<ulong>>(),
//                It.IsAny<bool>(),
//                It.IsAny<WriteOrderingType>(),
//                It.IsAny<ShardKeySelector>(),
//                It.IsAny<CancellationToken>()), 
//            Times.Once);
//    }

//    [Fact]
//    public async Task GetDocumentReferencesAsync_ReturnsDocumentReferences()
//    {
//        string docSourcePrefix = "docPrefix";
//        List<ScoredPoint> scoredPoints = new()
//        {
//            new ScoredPoint
//            {
//                Id = 1,
//                Payload =
//                {
//                    { "Content", new Value("content1") },
//                    { "DocumentReference", new Value("docRef1") }
//                },
//                Vectors = new Vectors { Vector = new float[] { 0.1f, 0.2f } }
//            }
//        };

//        qdrantClientMock
//            .Setup(client => 
//                client.QueryAsync(
//                    It.IsAny<string>(),
//                    It.IsAny<Query>(),
//                    It.IsAny<IReadOnlyList<PrefetchQuery>>(),
//                    It.IsAny<string>(),
//                    It.IsAny<Filter>(),
//                    It.IsAny<float>(),
//                    It.IsAny<SearchParams>(),
//                    It.IsAny<ulong>(),
//                    It.IsAny<ulong>(),
//                    It.IsAny<WithPayloadSelector>(),
//                    It.IsAny<WithVectorsSelector>(),
//                    It.IsAny<ReadConsistency>(),
//                    It.IsAny<ShardKeySelector>(),
//                    It.IsAny<LookupLocation>(),
//                    It.IsAny<TimeSpan>(),
//                    It.IsAny<CancellationToken>()))
//            .ReturnsAsync(scoredPoints);

//        List<TextChunk> result = await qdrantService.GetDocumentReferencesAsync(docSourcePrefix, CancellationToken.None);

//        Assert.Single(result);
//        Assert.Equal("content1", result[0].Content);
//        Assert.Equal("docRef1", result[0].DocumentReference);
//    }

//    [Fact]
//    public async Task UploadDocumentsAsync_UploadsDocumentsSuccessfully()
//    {
//        List<TextChunk> documents = new()
//        {
//            new TextChunk
//            {
//                ID = "1",
//                Content = "content1",
//                DocumentReference = "docRef1",
//                Embedding = new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f })
//            }
//        };

//        qdrantClientMock
//            .Setup(client => 
//                client.UpsertAsync(
//                    It.IsAny<string>(), 
//                    It.IsAny<IReadOnlyList<PointStruct>>(),
//                    It.IsAny<bool>(),
//                    It.IsAny<WriteOrderingType>(),
//                    It.IsAny<ShardKeySelector>(),
//                    It.IsAny<CancellationToken>()))
//            .ReturnsAsync(new UpdateResult());

//        await qdrantService.UploadDocumentsAsync(documents, CancellationToken.None);

//        qdrantClientMock.Verify(client => 
//            client.UpsertAsync(
//                "searchIndex", 
//                It.IsAny<IReadOnlyList<PointStruct>>(),
//                It.IsAny<bool>(),
//                It.IsAny<WriteOrderingType>(),
//                It.IsAny<ShardKeySelector>(),
//                It.IsAny<CancellationToken>()), 
//            Times.Once);
//    }

//    [Fact]
//    public async Task SearchAsync_ReturnsSearchResults()
//    {
//        ReadOnlyMemory<float> embedding = new(new float[] { 0.1f, 0.2f });
//        List<ScoredPoint> scoredPoints = new()
//        {
//            new ScoredPoint
//            {
//                Id = 1,
//                Payload =
//                {
//                    { "Content", new Value("content1") },
//                    { "DocumentReference", new Value("docRef1") }
//                },
//                Vectors = new Vectors { Vector = new float[] { 0.1f, 0.2f } }
//            }
//        };

//        qdrantClientMock
//            .Setup(client => 
//                client.QueryAsync(
//                    It.IsAny<string>(),
//                    It.IsAny<Query>(),
//                    It.IsAny<IReadOnlyList<PrefetchQuery>>(),
//                    It.IsAny<string>(),
//                    It.IsAny<Filter>(),
//                    It.IsAny<float>(),
//                    It.IsAny<SearchParams>(),
//                    It.IsAny<ulong>(),
//                    It.IsAny<ulong>(),
//                    It.IsAny<WithPayloadSelector>(),
//                    It.IsAny<WithVectorsSelector>(),
//                    It.IsAny<ReadConsistency>(),
//                    It.IsAny<ShardKeySelector>(),
//                    It.IsAny<LookupLocation>(),
//                    It.IsAny<TimeSpan>(),
//                    It.IsAny<CancellationToken>()))
//            .ReturnsAsync(scoredPoints);

//        List<TextChunk> result = await qdrantService.SearchAsync(embedding, CancellationToken.None);

//        Assert.Single(result);
//        Assert.Equal("content1", result[0].Content);
//        Assert.Equal("docRef1", result[0].DocumentReference);
//    }

//    [Fact]
//    public async Task SearchAsync_ThrowsGenerellemNeedsIngestionException_WhenIndexNotFound()
//    {
//        ReadOnlyMemory<float> embedding = new(new float[] { 0.1f, 0.2f });

//        qdrantClientMock
//            .Setup(client =>
//                client.QueryAsync(
//                    It.IsAny<string>(),
//                    It.IsAny<Query>(),
//                    It.IsAny<IReadOnlyList<PrefetchQuery>>(),
//                    It.IsAny<string>(),
//                    It.IsAny<Filter>(),
//                    It.IsAny<float>(),
//                    It.IsAny<SearchParams>(),
//                    It.IsAny<ulong>(),
//                    It.IsAny<ulong>(),
//                    It.IsAny<WithPayloadSelector>(),
//                    It.IsAny<WithVectorsSelector>(),
//                    It.IsAny<ReadConsistency>(),
//                    It.IsAny<ShardKeySelector>(),
//                    It.IsAny<LookupLocation>(),
//                    It.IsAny<TimeSpan>(),
//                    It.IsAny<CancellationToken>()))
//            .ThrowsAsync(new RequestFailedException((int)HttpStatusCode.NotFound, "Not Found"));

//        await Assert.ThrowsAsync<GenerellemNeedsIngestionException>(() => qdrantService.SearchAsync(embedding, CancellationToken.None));
//    }
//}
