namespace Generellem.DocumentSource;

public interface IDocumentSourceFactory
{
    IEnumerable<IDocumentSource> GetDocumentSources();
}
