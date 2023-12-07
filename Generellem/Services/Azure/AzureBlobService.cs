using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Configuration;

namespace Generellem.Services.Azure;

public class AzureBlobService : IAzureBlobService
{
    readonly IConfiguration config;

    readonly string? connStr;
    readonly string? container;

    public AzureBlobService(IConfiguration config)
    {
        this.config = config;

        connStr = config[GKeys.AzBlobConnectionString];
        container = config[GKeys.AzBlobContainer];
    }

    public virtual async Task UploadAsync(string fileName, Stream stream, CancellationToken cancelToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connStr, nameof(connStr));
        ArgumentException.ThrowIfNullOrWhiteSpace(container, nameof(container));
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        var blobClient = new BlobClient(connStr, container, fileName);
        await blobClient.UploadAsync(stream, overwrite: true, cancelToken);
    }

    public virtual async Task<Stream> DownloadAsync(string fileName, CancellationToken cancelToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connStr, nameof(connStr));
        ArgumentException.ThrowIfNullOrWhiteSpace(container, nameof(container));
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));

        var blobClient = new BlobClient(connStr, container, fileName);
        BlobDownloadInfo blobInfo = await blobClient.DownloadAsync(cancelToken);

        return blobInfo.Content;
    }

    public virtual async Task DeleteAsync(string fileName, CancellationToken cancelToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connStr, nameof(connStr));
        ArgumentException.ThrowIfNullOrWhiteSpace(container, nameof(container));
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));

        var blobClient = new BlobClient(connStr, container, fileName);
        await blobClient.DeleteAsync(cancellationToken: cancelToken);
    }
}
