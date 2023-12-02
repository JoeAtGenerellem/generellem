using System.Security;

using Generellem.Rag;
using Generellem.Rag.AzureOpenAI;
using Generellem.Security;
using Generellem.Services.Azure;

using Moq;

namespace Generellem.Tests;
public class AzureOpenAIRagTests
{
    Mock<ISecretStore> secretStoreMock = new();
    Mock<IAzureSearchService> searchSvc = new();

    IRag azureOpenAIRag;

    public AzureOpenAIRagTests()
    {
        azureOpenAIRag = new AzureOpenAIRag(secretStoreMock.Object, searchSvc.Object);
    }

    [Fact]
    public async Task IndexAsync_CallsCreateIndex()
    {
        var chunks = new List<TextChunk>();

        await azureOpenAIRag.IndexAsync(chunks, CancellationToken.None);

        searchSvc.Verify(x => x.CreateIndexAsync(), Times.Once());
    }

    [Fact]
    public async Task IndexAsync_CallsUploadDocuments()
    {
        var chunks = new List<TextChunk>();

        await azureOpenAIRag.IndexAsync(chunks, CancellationToken.None);

        searchSvc.Verify(x => x.UploadDocumentsAsync(chunks), Times.Once());
    }

    [Fact]
    public async Task IndexAsync_CallsUploadDocumentsWithCorrectChunks()
    {
        var chunks = new List<TextChunk> { /* populate */ };

        await azureOpenAIRag.IndexAsync(chunks, CancellationToken.None);

        searchSvc.Verify(x => x.UploadDocumentsAsync(It.Is<List<TextChunk>>(c => c == chunks)), Times.Once());
    }
}
