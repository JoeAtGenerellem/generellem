using Azure;
using Azure.Search.Documents.Indexes;

namespace Generellem.Services;

public class AzureSearchService : IAzureSearchService
{
    readonly string searchServiceAdminApiKey;
    readonly string searchServiceEndpoint;
    readonly string searchServiceIndexer;

    public AzureSearchService()
    {
        searchServiceAdminApiKey = Environment.GetEnvironmentVariable("GenerellemSearchServiceAdminApiKey")!;
        searchServiceEndpoint = Environment.GetEnvironmentVariable("GenerellemSearchServiceEndpoint")!;
        searchServiceIndexer = Environment.GetEnvironmentVariable("GenerellemSearchServiceIndexer")!;
    }

    public async Task RunIndexerAsync()
    {
        Uri endpoint = new(searchServiceEndpoint);
        AzureKeyCredential credential = new AzureKeyCredential(searchServiceAdminApiKey);

        SearchIndexerClient searchIndexerClient = new SearchIndexerClient(endpoint, credential);

        await searchIndexerClient.RunIndexerAsync(searchServiceIndexer);
    }
}
