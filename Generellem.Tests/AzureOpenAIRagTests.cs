using Generellem.Rag;
using Generellem.Rag.AzureOpenAI;
using Generellem.Services.Azure;

using Microsoft.Extensions.Configuration;

using Moq;

namespace Generellem.Tests;

public class AzureOpenAIRagTests
{
    Mock<IAzureSearchService> azSearchSvcMock = new();
    Mock<IConfiguration> configMock = new();

    IRag azureOpenAIRag;

    public AzureOpenAIRagTests()
    {
        azureOpenAIRag = new AzureOpenAIRag(azSearchSvcMock.Object, configMock.Object);
    }

    [Fact]
    public async Task IndexAsync_CallsCreateIndex()
    {
        var chunks = new List<TextChunk>();

        await azureOpenAIRag.IndexAsync(chunks, CancellationToken.None);

        azSearchSvcMock.Verify(x => x.CreateIndexAsync(), Times.Once());
    }

    [Fact]
    public async Task IndexAsync_CallsUploadDocuments()
    {
        var chunks = new List<TextChunk>();

        await azureOpenAIRag.IndexAsync(chunks, CancellationToken.None);

        azSearchSvcMock.Verify(x => x.UploadDocumentsAsync(chunks), Times.Once());
    }

    [Fact]
    public async Task IndexAsync_CallsUploadDocumentsWithCorrectChunks()
    {
        var chunks = new List<TextChunk> { /* populate */ };

        await azureOpenAIRag.IndexAsync(chunks, CancellationToken.None);

        azSearchSvcMock.Verify(x => x.UploadDocumentsAsync(It.Is<List<TextChunk>>(c => c == chunks)), Times.Once());
    }
}
