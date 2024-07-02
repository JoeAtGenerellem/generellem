namespace Generellem.DocumentSource;

/// <summary>
/// Gets instances associated with <see cref="IDocumentSource"/>.
/// </summary>
public interface IDocumentSourceFactory
{
    /// <summary>
    /// List of document sources supported by this app.
    /// </summary>
    /// <returns><see cref="IDocumentSource"/></returns>
    IEnumerable<IDocumentSource> GetDocumentSources();
}
