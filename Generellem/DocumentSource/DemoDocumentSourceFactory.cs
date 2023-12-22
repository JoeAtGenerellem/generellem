namespace Generellem.DocumentSource;

public class DemoDocumentSourceFactory : IDocumentSourceFactory
{
    public IEnumerable<IDocumentSource> GetDocumentSources() => 
        [
            new FileSystem(),
            new Website()
        ];
}
