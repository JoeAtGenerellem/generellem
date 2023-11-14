using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Generellem.Services;

public class AzureBlobService : IAzureBlobService
{
    private readonly string _connectionString;
    private readonly string _containerName;

    public AzureBlobService(string connectionString, string containerName)
    {
        _connectionString = connectionString;
        _containerName = /*"fileupload-learnopenaisearchidx2";//*/ containerName;
    }

    public async Task UploadAsync(string fileName, Stream stream)
    {
        var blobClient = new BlobClient(_connectionString, _containerName, fileName);
        await blobClient.UploadAsync(stream, overwrite: true);
    }

    public async Task<Stream> DownloadAsync(string fileName)
    {
        var blobClient = new BlobClient(_connectionString, _containerName, fileName);
        BlobDownloadInfo blobInfo = await blobClient.DownloadAsync();

        return blobInfo.Content;
    }

    public async Task DeleteAsync(string fileName)
    {
        var blobClient = new BlobClient(_connectionString, _containerName, fileName);
        await blobClient.DeleteAsync();
    }
}
