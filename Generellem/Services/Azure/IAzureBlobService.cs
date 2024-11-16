using Azure.Storage.Blobs.Models;

namespace Generellem.Services.Azure;

public interface IAzureBlobService
{
    Task DeleteAsync(string connStr, string container, string fileName, CancellationToken cancelToken);
    Task<Stream> DownloadAsync(string connStr, string container, string fileName, CancellationToken cancelToken);
    Task<BlobContentInfo> UploadAsync(string connStr, string container, string fileName, Stream stream, CancellationToken cancelToken);
}