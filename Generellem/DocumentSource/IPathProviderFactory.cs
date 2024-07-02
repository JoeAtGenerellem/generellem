namespace Generellem.DocumentSource;

public interface IPathProviderFactory
{
    IPathProvider Create(IDocumentSource docSource);
}
