using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;

using Generellem.Rag;

using Microsoft.Extensions.Configuration;

namespace Generellem.Services.Azure;

public class AzureSearchService : IAzureSearchService
{
    const int VectorSearchDimensions = 1536;
    const string VectorAlgorithmConfigName = "hnsw-config";
    const string VectorProfileName = "generellem-vector-profile";

    readonly IConfiguration config;

    readonly string? searchServiceAdminApiKey;
    readonly string? searchServiceEndpoint;
    readonly string? searchServiceIndex;

    public AzureSearchService(IConfiguration config)
    {
        this.config = config;

        searchServiceAdminApiKey = config[GKeys.AzSearchServiceAdminApiKey];
        searchServiceEndpoint = config[GKeys.AzSearchServiceEndpoint];
        searchServiceIndex = config[GKeys.AzSearchServiceIndex];
    }

    public virtual async Task CreateIndexAsync()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchServiceAdminApiKey, nameof(searchServiceAdminApiKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(searchServiceEndpoint, nameof(searchServiceEndpoint));

        Uri endpoint = new(searchServiceEndpoint);
        AzureKeyCredential credential = new AzureKeyCredential(searchServiceAdminApiKey);

        SearchIndex searchIndex = new(searchServiceIndex)
        {
            Fields =
            {
                new SimpleField(nameof(TextChunk.ID), SearchFieldDataType.String) { IsKey = true, IsFilterable = true, IsSortable = true, IsFacetable = true },
                new SearchableField(nameof(TextChunk.FileRef)) { IsFilterable = true, IsSortable = true },
                new SearchableField(nameof(TextChunk.Content)) { IsFilterable = true },
                new VectorSearchField(nameof(TextChunk.Embedding), VectorSearchDimensions, VectorProfileName)
            },
            VectorSearch = new()
            {
                Profiles =
                {
                    new VectorSearchProfile(VectorProfileName, VectorAlgorithmConfigName)
                },
                Algorithms =
                {
                    new HnswAlgorithmConfiguration(VectorAlgorithmConfigName)
                }
            },
        };
        SearchIndexClient indexClient = new(endpoint, credential);

        await indexClient.CreateOrUpdateIndexAsync(searchIndex);
    }

    public virtual async Task UploadDocumentsAsync(List<TextChunk> documents)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchServiceAdminApiKey, nameof(searchServiceAdminApiKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(searchServiceEndpoint, nameof(searchServiceEndpoint));

        Uri endpoint = new(searchServiceEndpoint);
        AzureKeyCredential credential = new AzureKeyCredential(searchServiceAdminApiKey);

        SearchClient searchClient = new SearchClient(endpoint, searchServiceIndex, credential);

        await searchClient.IndexDocumentsAsync(IndexDocumentsBatch.MergeOrUpload(documents));
    }

    public virtual async Task<List<TResponse>> SearchAsync<TResponse>(ReadOnlyMemory<float> embedding)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchServiceAdminApiKey, nameof(searchServiceAdminApiKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(searchServiceEndpoint, nameof(searchServiceEndpoint));

        Uri endpoint = new(searchServiceEndpoint);
        AzureKeyCredential credential = new AzureKeyCredential(searchServiceAdminApiKey);

        SearchClient searchClient = new SearchClient(endpoint, searchServiceIndex, credential);

        var searchOptions = new SearchOptions
        {
            VectorSearch = new()
            {
                Queries = { new VectorizedQuery(embedding) { KNearestNeighborsCount = 3, Fields = { nameof(TextChunk.Embedding) } } }
            }
        };

        SearchResults<TResponse> results = await searchClient.SearchAsync<TResponse>(searchOptions);

        List<TResponse> chunks =
            (from chunk in results.GetResultsAsync().ToBlockingEnumerable()
             select chunk.Document)
            .ToList();

        return chunks;
    }
}
