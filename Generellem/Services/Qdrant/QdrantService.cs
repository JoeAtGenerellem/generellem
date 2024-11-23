﻿using Generellem.Embedding;
using Generellem.Services.Exceptions;

using Grpc.Core;

using Microsoft.Extensions.Logging;

using Polly;
using Polly.Retry;

using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Generellem.Services.Azure;

public class QdrantService(IDynamicConfiguration config, ILogger<QdrantService> logger) : ISearchService
{
    const int VectorSearchDimensions = 1536; // defined by text-embedding-ada-002
    const string DocumentReference = "DocumentReference";
    const string ID = "ID";
    const string SourceReference = "SourceReference";
    const string Content = "Content";
    const string TenantID = "TenantID";
    const string GroupID = "GroupID";

    string? QdrantApiKey => config[GKeys.QdrantApiKey];
    string? QdrantEndpoint => config[GKeys.QdrantEndpoint]; //"http://localhost:6334";
    string QdrantTenantID => config[GKeys.TenantID] ?? "0";
    string QdrantGroupID => config[GKeys.GroupID] ?? "0";
    string? QdrantCollection => config[GKeys.QdrantCollection];

    bool collectionExists = false;

    readonly ResiliencePipeline pipeline = 
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions())
            .AddTimeout(TimeSpan.FromSeconds(3))
            .Build();

    public virtual async Task CreateIndexAsync(CancellationToken cancelToken)
    {
        if (await DoesIndexExistAsync(cancelToken))
            return;

        ArgumentException.ThrowIfNullOrWhiteSpace(QdrantApiKey, nameof(QdrantApiKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(QdrantEndpoint, nameof(QdrantEndpoint));
        ArgumentException.ThrowIfNullOrWhiteSpace(QdrantCollection, nameof(QdrantCollection));

        try
        {
            Uri endpoint = new(QdrantEndpoint);
            QdrantClient client = new(endpoint.Host, apiKey: QdrantApiKey, https: QdrantEndpoint.StartsWith("https"));

            await pipeline.ExecuteAsync(
                async token =>
                    await client.CreateCollectionAsync(
                        collectionName: QdrantCollection,
                        vectorsConfig: new VectorParams { Size = VectorSearchDimensions, Distance = Distance.Cosine },
                        hnswConfig: new HnswConfigDiff { PayloadM = 16, M = 0 }),
                cancelToken);

            await client.CreatePayloadIndexAsync(
	            collectionName: QdrantCollection,
	            fieldName: TenantID,
	            schemaType: PayloadSchemaType.Keyword,
	            indexParams: new PayloadIndexParams
	            {
		            KeywordIndexParams = new KeywordIndexParams
		            {
			            IsTenant = true
		            }
	            });
            await client.CreatePayloadIndexAsync(
	            collectionName: QdrantCollection,
	            fieldName: GroupID,
	            schemaType: PayloadSchemaType.Keyword);
        }
        catch (RpcException rpcEx)
        {
            logger.LogError(GenerellemLogEvents.AuthorizationFailure, rpcEx, "Please check credentials and exception details for more info.");
            throw;
        }    
    }
    
    public async Task<bool> DoesIndexExistAsync(CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(QdrantApiKey, nameof(QdrantApiKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(QdrantEndpoint, nameof(QdrantEndpoint));
        ArgumentException.ThrowIfNullOrWhiteSpace(QdrantCollection, nameof(QdrantCollection));

        try
        {
            if (collectionExists)
                return true;

            Uri endpoint = new(QdrantEndpoint);
            QdrantClient client = new(endpoint.Host, apiKey: QdrantApiKey, https: QdrantEndpoint.StartsWith("https"));

            collectionExists = await client.CollectionExistsAsync(QdrantCollection);
        }
        catch (RpcException rpcEx) when (rpcEx.StatusCode == StatusCode.NotFound || rpcEx.StatusCode == StatusCode.Unimplemented)
        {
            // 404 indicates the index does not exist
            collectionExists = false;
        }
            
        return collectionExists;
    }

    public async Task DeleteDocumentReferencesAsync(List<string> idsToDelete, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(QdrantApiKey, nameof(QdrantApiKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(QdrantEndpoint, nameof(QdrantEndpoint));
        ArgumentException.ThrowIfNullOrWhiteSpace(QdrantCollection, nameof(QdrantCollection));

        IReadOnlyList<Guid> ids = idsToDelete.Select(id => Guid.Parse(id)).ToList();
        try
        {
            Uri endpoint = new(QdrantEndpoint);
            QdrantClient client = new(endpoint.Host, apiKey: QdrantApiKey, https: QdrantEndpoint.StartsWith("https"));

            UpdateResult result =
                await pipeline.ExecuteAsync(
                    async token => await client.DeleteAsync(QdrantCollection, ids),
                    cancellationToken);
        }
        catch (RpcException rpcEx) when (rpcEx.StatusCode == StatusCode.NotFound)
        {
            // 404 indicates the index does not exist
        }
    }
     
    public async Task<List<TextChunk>> GetDocumentReferenceAsync(string documentReference, CancellationToken cancellationToken)
    {
        if (!await DoesIndexExistAsync(cancellationToken))
            return new();

        ArgumentException.ThrowIfNullOrWhiteSpace(QdrantApiKey, nameof(QdrantApiKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(QdrantEndpoint, nameof(QdrantEndpoint));
        ArgumentException.ThrowIfNullOrWhiteSpace(QdrantCollection, nameof(QdrantCollection));

        try
        {    
            Uri endpoint = new(QdrantEndpoint);
            QdrantClient client = new(endpoint.Host, apiKey: QdrantApiKey, https: QdrantEndpoint.StartsWith("https"));

            Filter filter = new()
		    {
			    Must =
			    {
				    new Condition
				    {
					    Field = new FieldCondition { Key = DocumentReference, Match = new Match { Keyword = documentReference } },
				    },
				    new Condition
				    {
					    Field = new FieldCondition { Key = TenantID, Match = new Match { Keyword = QdrantTenantID } },
				    },
				    new Condition
				    {
					    Field = new FieldCondition { Key = GroupID, Match = new Match { Keyword = QdrantGroupID } },
				    },
			    }
		    };

            WithPayloadSelector payloadSelector =
                new()
                {
                    Include = new PayloadIncludeSelector
                    {
                        Fields = { new string[] { ID, DocumentReference } }
                    }
                };

            IReadOnlyList<ScoredPoint> queryResult =
                await pipeline.ExecuteAsync(
                    async token => await client.QueryAsync(
                        collectionName: QdrantCollection,
                        filter: filter,
                        payloadSelector: true,
                        vectorsSelector: true),
                    cancellationToken);

            List<TextChunk> chunks =
                (from doc in queryResult
                 select new TextChunk
                 {
                     ID = doc.Id.Uuid,
                     DocumentReference = doc.Payload[DocumentReference].StringValue
                 })
                .ToList();

            return chunks;
        }
        catch (RpcException rpcEx)
        {
            logger.LogError(GenerellemLogEvents.AuthorizationFailure, rpcEx, "Please check credentials and exception details for more info.");
            throw;
        }
    }
   
    public async Task<List<TextChunk>> GetDocumentReferencesAsync(string docSourcePrefix, CancellationToken cancellationToken)
    {
        if (!await DoesIndexExistAsync(cancellationToken))
            return new();

        ArgumentException.ThrowIfNullOrWhiteSpace(QdrantApiKey, nameof(QdrantApiKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(QdrantEndpoint, nameof(QdrantEndpoint));
        ArgumentException.ThrowIfNullOrWhiteSpace(QdrantCollection, nameof(QdrantCollection));

        try
        {    
            Uri endpoint = new(QdrantEndpoint);
            QdrantClient client = new(endpoint.Host, apiKey: QdrantApiKey, https: QdrantEndpoint.StartsWith("https"));

            Filter filter = new()
		    {
			    Must =
			    {
				    new Condition
				    {
					    Field = new FieldCondition { Key = SourceReference, Match = new Match { Keyword = docSourcePrefix } },
				    },
				    new Condition
				    {
					    Field = new FieldCondition { Key = TenantID, Match = new Match { Keyword = QdrantTenantID } },
				    },
				    new Condition
				    {
					    Field = new FieldCondition { Key = GroupID, Match = new Match { Keyword = QdrantGroupID } },
				    },
			    }
		    };

            WithPayloadSelector payloadSelector =
                new()
                {
                    Include = new PayloadIncludeSelector
                    {
                        Fields = { new string[] { ID, DocumentReference } }
                    }
                };

            IReadOnlyList<ScoredPoint> queryResult =
                await pipeline.ExecuteAsync(
                    async token => await client.QueryAsync(
                        collectionName: QdrantCollection,
                        filter: filter,
                        payloadSelector: true,
                        vectorsSelector: true),
                    cancellationToken);

            List<TextChunk> chunks =
                (from doc in queryResult
                 select new TextChunk
                 {
                     ID = doc.Id.Uuid,
                     DocumentReference = doc.Payload[DocumentReference].StringValue
                 })
                .ToList();

            return chunks;
        }
        catch (RpcException rpcEx)
        {
            logger.LogError(GenerellemLogEvents.AuthorizationFailure, rpcEx, "Please check credentials and exception details for more info.");
            throw;
        }
    }

    public async Task<List<TextChunk>> GetSourceReferencesAsync(string sourceReference, CancellationToken cancellationToken)
    {
        if (!await DoesIndexExistAsync(cancellationToken))
            return new();

        ArgumentException.ThrowIfNullOrWhiteSpace(QdrantApiKey, nameof(QdrantApiKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(QdrantEndpoint, nameof(QdrantEndpoint));
        ArgumentException.ThrowIfNullOrWhiteSpace(QdrantCollection, nameof(QdrantCollection));

        try
        {    
            Uri endpoint = new(QdrantEndpoint);
            QdrantClient client = new(endpoint.Host, apiKey: QdrantApiKey, https: QdrantEndpoint.StartsWith("https"));

            Filter filter = new()
		    {
			    Must =
			    {
				    new Condition
				    {
					    Field = new FieldCondition { Key = SourceReference, Match = new Match { Keyword = sourceReference } },
				    },
				    new Condition
				    {
					    Field = new FieldCondition { Key = TenantID, Match = new Match { Keyword = QdrantTenantID } },
				    },
				    new Condition
				    {
					    Field = new FieldCondition { Key = GroupID, Match = new Match { Keyword = QdrantGroupID } },
				    },
			    }
		    };

            WithPayloadSelector payloadSelector =
                new()
                {
                    Include = new PayloadIncludeSelector
                    {
                        Fields = { new string[] { ID, DocumentReference } }
                    }
                };

            IReadOnlyList<ScoredPoint> queryResult =
                await pipeline.ExecuteAsync(
                    async token => await client.QueryAsync(
                        collectionName: QdrantCollection,
                        filter: filter,
                        payloadSelector: true,
                        vectorsSelector: true),
                    cancellationToken);

            List<TextChunk> chunks =
                (from doc in queryResult
                 select new TextChunk
                 {
                     ID = doc.Id.Uuid,
                     DocumentReference = doc.Payload[DocumentReference].StringValue
                 })
                .ToList();

            return chunks;
        }
        catch (RpcException rpcEx)
        {
            logger.LogError(GenerellemLogEvents.AuthorizationFailure, rpcEx, "Please check credentials and exception details for more info.");
            throw;
        }
    }

    public virtual async Task UploadDocumentsAsync(List<TextChunk> documents, CancellationToken cancelToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(QdrantApiKey, nameof(QdrantApiKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(QdrantEndpoint, nameof(QdrantEndpoint));
        ArgumentException.ThrowIfNullOrWhiteSpace(QdrantCollection, nameof(QdrantCollection));

        try
        {
            IReadOnlyList<PointStruct> points =
                (from doc in documents
                 select new PointStruct
                 {
                     Id = new PointId() { Uuid = doc.ID! },
                     Payload = 
                     {
                        [DocumentReference] = doc.DocumentReference!,
                        [SourceReference] = doc.SourceReference!,
                        [Content] = doc.Content!,
                        [TenantID] = QdrantTenantID ?? string.Empty,
                        [GroupID] = QdrantGroupID ?? string.Empty
                     },
                     Vectors = doc.Embedding.ToArray()
                 })
                .ToList();

            Uri endpoint = new(QdrantEndpoint);
            QdrantClient client = new(endpoint.Host, apiKey: QdrantApiKey, https: QdrantEndpoint.StartsWith("https"));

            await pipeline.ExecuteAsync(
                async token => await client.UpsertAsync(QdrantCollection, points),
                cancelToken);
        }
        catch (RpcException rpcEx)
        {
            logger.LogError(GenerellemLogEvents.AuthorizationFailure, rpcEx, "Please check credentials and exception details for more info.");
            throw;
        }    
    }

    public virtual async Task<List<TextChunk>> SearchAsync(ReadOnlyMemory<float> embedding, CancellationToken cancelToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(QdrantApiKey, nameof(QdrantApiKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(QdrantEndpoint, nameof(QdrantEndpoint));
        ArgumentException.ThrowIfNullOrWhiteSpace(QdrantCollection, nameof(QdrantCollection));

        const int ResponseCountLimit = 3;

        try
        {
            Filter filter = new()
		    {
			    Must =
                {
				    new Condition
				    {
					    Field = new FieldCondition { Key = TenantID, Match = new Match { Keyword = QdrantTenantID } },
				    },
				    new Condition
				    {
					    Field = new FieldCondition { Key = GroupID, Match = new Match { Keyword = QdrantGroupID } },
				    },
			    }
		    };

            Uri endpoint = new(QdrantEndpoint);
            QdrantClient client = new(endpoint.Host, apiKey: QdrantApiKey, https: QdrantEndpoint.StartsWith("https"));

            IReadOnlyList<ScoredPoint> queryResult = await pipeline.ExecuteAsync(
                async token =>
                {
                    return await client.SearchAsync(
                        collectionName: QdrantCollection,
                        filter: filter,
                        vector: embedding.ToArray(),
                        limit: ResponseCountLimit);
                },
                cancelToken);

            List<TextChunk> chunks =
                (from doc in queryResult
                 select new TextChunk
                 {
                     ID = doc.Id.Uuid,
                     Content = doc.Payload[Content].StringValue,
                     DocumentReference = doc.Payload[DocumentReference].StringValue,
                 })
                .ToList();

            return chunks;
        }
        catch (RpcException rpcEx) when (rpcEx.StatusCode == StatusCode.NotFound)
        {
            throw new GenerellemNeedsIngestionException(
                "You need to perform ingestion before querying so that there are documents available for context.",  
                rpcEx);
        }
    }
}
