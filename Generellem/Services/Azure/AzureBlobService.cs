using Azure.AI.OpenAI;
using System.Threading;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Configuration;

using Polly;
using Polly.Retry;
using System.IO;

namespace Generellem.Services.Azure;

public class AzureBlobService : IAzureBlobService
{
    readonly IConfiguration config;

    readonly string? connStr;
    readonly string? container;

    readonly ResiliencePipeline pipeline;

    public AzureBlobService(IConfiguration config)
    {
        this.config = config;

        connStr = config[GKeys.AzBlobConnectionString];
        container = config[GKeys.AzBlobContainer];

        pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions())
            .AddTimeout(TimeSpan.FromSeconds(3))
            .Build();
    }

    public virtual async Task UploadAsync(string fileName, Stream stream, CancellationToken cancelToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connStr, nameof(connStr));
        ArgumentException.ThrowIfNullOrWhiteSpace(container, nameof(container));
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        var blobClient = new BlobClient(connStr, container, fileName);

        await pipeline.ExecuteAsync(
            async token => await blobClient.UploadAsync(stream, overwrite: true, token),
            cancelToken);
    }

    public virtual async Task<Stream> DownloadAsync(string fileName, CancellationToken cancelToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connStr, nameof(connStr));
        ArgumentException.ThrowIfNullOrWhiteSpace(container, nameof(container));
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));

        var blobClient = new BlobClient(connStr, container, fileName);

        BlobDownloadInfo blobInfo = await pipeline.ExecuteAsync<BlobDownloadInfo>(
            async token => await blobClient.DownloadAsync(cancelToken),
            cancelToken);

        return blobInfo.Content;
    }

    public virtual async Task DeleteAsync(string fileName, CancellationToken cancelToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connStr, nameof(connStr));
        ArgumentException.ThrowIfNullOrWhiteSpace(container, nameof(container));
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));

        var blobClient = new BlobClient(connStr, container, fileName);

        await pipeline.ExecuteAsync(
            async token => await blobClient.DeleteAsync(cancellationToken: token),
            cancelToken);
    }
}
