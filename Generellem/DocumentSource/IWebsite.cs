using System.Runtime.CompilerServices;

namespace Generellem.DocumentSource;
public interface IWebsite
{
    string Description { get; set; }
    string Prefix { get; init; }

    IAsyncEnumerable<DocumentInfo> GetDocumentsAsync(CancellationToken cancelToken);
    Task<IEnumerable<WebSpec>> GetWebsitesAsync(string configPath = nameof(Website) + ".json");
    ValueTask WriteSitesAsync(IEnumerable<WebSpec> webSpecs, string configPath = "Website.json");
}