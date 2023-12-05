using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Generellem.Services.Azure;

public class AzureBlobService : IAzureBlobService
{
    private readonly string _connectionString;
    private readonly string _containerName;

    public AzureBlobService(string connectionString, string containerName)
    {
        _connectionString = connectionString;
        _containerName = /*"fileupload-learnopenaisearchidx2";//*/ containerName;
    }

    public virtual async Task UploadAsync(string fileName, Stream stream)
    {
        var blobClient = new BlobClient(_connectionString, _containerName, fileName);
        await blobClient.UploadAsync(stream, overwrite: true);
    }

    public virtual async Task<Stream> DownloadAsync(string fileName)
    {
        var blobClient = new BlobClient(_connectionString, _containerName, fileName);
        BlobDownloadInfo blobInfo = await blobClient.DownloadAsync();

        return blobInfo.Content;
    }

    public virtual async Task DeleteAsync(string fileName)
    {
        var blobClient = new BlobClient(_connectionString, _containerName, fileName);
        await blobClient.DeleteAsync();
    }
}
