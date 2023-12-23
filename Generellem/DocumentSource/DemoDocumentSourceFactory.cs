using Generellem.Services;

namespace Generellem.DocumentSource;

public class DemoDocumentSourceFactory : IDocumentSourceFactory
{
    readonly IHttpClientFactory httpFact;

    public DemoDocumentSourceFactory(IHttpClientFactory httpFact)
    {
        this.httpFact = httpFact;
    }

    public IEnumerable<IDocumentSource> GetDocumentSources() => 
        [
            new FileSystem(),
            new Website(httpFact)
        ];
}
