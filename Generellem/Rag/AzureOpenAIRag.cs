using Generellem.Document.DocumentTypes;
using Generellem.RAG;

namespace Generellem.Rag;

public class AzureOpenAIRag : IRag
{
    public async Task Embedsync(Stream documentStream, IDocumentType docType, CancellationToken cancellationToken)
    {

    }

    public async Task IndexAsync(CancellationToken cancellationToken)
    {

    }

    public async Task<List<string>> SearchAsync(string text, CancellationToken cancellationToken)
    {
        return new();
    }
}
