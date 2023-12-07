namespace Generellem.Services.Azure;

public interface IAzureBlobService
{
    Task DeleteAsync(string fileName, CancellationToken cancelToken);
    Task<Stream> DownloadAsync(string fileName, CancellationToken cancelToken);
    Task UploadAsync(string fileName, Stream stream, CancellationToken cancelToken);
}