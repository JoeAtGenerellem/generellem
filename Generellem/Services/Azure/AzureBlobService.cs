using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Polly;
using Polly.Retry;

namespace Generellem.Services.Azure;

public class AzureBlobService : IAzureBlobService
{
    readonly ResiliencePipeline pipeline =
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions())
            .AddTimeout(TimeSpan.FromSeconds(3))
            .Build();

    public virtual async Task UploadAsync(string connStr, string container, string fileName, Stream stream, CancellationToken cancelToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connStr, nameof(connStr));
        ArgumentException.ThrowIfNullOrWhiteSpace(container, nameof(container));
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        BlobClient blobClient = new(connStr, container, fileName);

        await pipeline.ExecuteAsync(
            async token => await blobClient.UploadAsync(stream, overwrite: true, token),
            cancelToken);
    }

    public virtual async Task<Stream> DownloadAsync(string connStr, string container, string fileName, CancellationToken cancelToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connStr, nameof(connStr));
        ArgumentException.ThrowIfNullOrWhiteSpace(container, nameof(container));
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));

        BlobClient blobClient = new(connStr, container, fileName);

        BlobDownloadInfo blobInfo = await pipeline.ExecuteAsync<BlobDownloadInfo>(
            async token => await blobClient.DownloadAsync(cancelToken),
            cancelToken);

        return blobInfo.Content;
    }

    public virtual async Task DeleteAsync(string connStr, string container, string fileName, CancellationToken cancelToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connStr, nameof(connStr));
        ArgumentException.ThrowIfNullOrWhiteSpace(container, nameof(container));
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));

        BlobClient blobClient = new(connStr, container, fileName);

        await pipeline.ExecuteAsync(
            async token => await blobClient.DeleteAsync(cancellationToken: token),
            cancelToken);
    }
}
