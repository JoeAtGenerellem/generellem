using Generellem.Services;

namespace Generellem.DocumentSource;

/// <summary>
/// These are the document sources that are from MSGraph.
/// </summary>
/// <param name="config">Configuration parameter source.</param>
public class MSGraphDocumentSourceFactory(
    IDynamicConfiguration config,
    IMSGraphClientFactory msGraphFact,
    IPathProviderFactory pathProviderFact)
    : IDocumentSourceFactory
{
    /// <summary>
    /// Returns a list of MSGraph document sources that are currently supported.
    /// </summary>
    /// <returns>List of <see cref="IDocumentSource"/></returns>
    public IEnumerable<IDocumentSource> GetDocumentSources() =>
        [
            new OneDriveFileSystem(config[GKeys.BaseUrl]!, config[GKeys.UserID]!, msGraphFact, pathProviderFact)
        ];
}
