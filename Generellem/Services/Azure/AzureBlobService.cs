using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Logging;

using Polly;
using Polly.Retry;

namespace Generellem.Services.Azure;

public class AzureBlobService(IGenerellemConfiguration config, ILogger<AzureBlobService> logger) : IAzureBlobService
{
    readonly ILogger<AzureBlobService> logger = logger;

    readonly string? connStr = config[GKeys.AzBlobConnectionString];
    readonly string? container = config[GKeys.AzBlobContainer];

    readonly ResiliencePipeline pipeline = 
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions())
            .AddTimeout(TimeSpan.FromSeconds(3))
            .Build();

    public virtual async Task UploadAsync(string fileName, Stream stream, CancellationToken cancelToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connStr, nameof(connStr));
        ArgumentException.ThrowIfNullOrWhiteSpace(container, nameof(container));
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        var blobClient = new BlobClient(connStr, container, fileName);

        try
        {
            await pipeline.ExecuteAsync(
        async token => await blobClient.UploadAsync(stream, overwrite: true, token),
        cancelToken);

        }
        catch (RequestFailedException rfEx)
        {
            logger.LogError(GenerellemLogEvents.AuthorizationFailure, rfEx, "Please check credentials and exception details for more info.");
            throw;
        }    
    }

    public virtual async Task<Stream> DownloadAsync(string fileName, CancellationToken cancelToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connStr, nameof(connStr));
        ArgumentException.ThrowIfNullOrWhiteSpace(container, nameof(container));
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));

        var blobClient = new BlobClient(connStr, container, fileName);

        try
        {
            BlobDownloadInfo blobInfo = await pipeline.ExecuteAsync<BlobDownloadInfo>(
                async token => await blobClient.DownloadAsync(cancelToken),
                cancelToken);

            return blobInfo.Content;
        }
        catch (RequestFailedException rfEx)
        {
            logger.LogError(GenerellemLogEvents.AuthorizationFailure, rfEx, "Please check credentials and exception details for more info.");
            throw;
        }    
    }

    public virtual async Task DeleteAsync(string fileName, CancellationToken cancelToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connStr, nameof(connStr));
        ArgumentException.ThrowIfNullOrWhiteSpace(container, nameof(container));
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));

        var blobClient = new BlobClient(connStr, container, fileName);

        try
        {
            await pipeline.ExecuteAsync(
                async token => await blobClient.DeleteAsync(cancellationToken: token),
                cancelToken);

        }
        catch (RequestFailedException rfEx) 
        { 
            logger.LogError(GenerellemLogEvents.AuthorizationFailure, rfEx, "Please check credentials and exception details for more info."); 
            throw; 
        }    
    }
}
