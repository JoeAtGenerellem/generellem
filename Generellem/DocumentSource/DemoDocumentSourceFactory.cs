using Generellem.Services;

using Microsoft.Extensions.Logging;

using IHttpClientFactory = Generellem.Services.IHttpClientFactory;

namespace Generellem.DocumentSource;

public class DemoDocumentSourceFactory(
    IHttpClientFactory httpFact, ILogger<Website> logger) 
    : IDocumentSourceFactory
{
    readonly IHttpClientFactory httpFact = httpFact;
    readonly ILogger<Website> logger = logger;

    public IEnumerable<IDocumentSource> GetDocumentSources() => 
        [
            new FileSystem(),
            new Website(httpFact, logger)
        ];
}
