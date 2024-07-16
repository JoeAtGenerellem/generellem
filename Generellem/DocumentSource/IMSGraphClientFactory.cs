using Microsoft.Graph;

namespace Generellem.DocumentSource;

/// <summary>
/// Instantiates MSGraph types.
/// </summary>
public interface IMSGraphClientFactory
{
    /// <summary>
    /// Instantiates a new <see cref="GraphServiceClient"/> for accessing MSGraph.
    /// </summary>
    /// <returns><see cref="GraphServiceClient"/></returns>
    Task<GraphServiceClient> CreateAsync(string scopes);
}