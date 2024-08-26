namespace Generellem.Services.Azure;

public interface IAzureBlobService
{
    Task DeleteAsync(string connStr, string container, string fileName, CancellationToken cancelToken);
    Task<Stream> DownloadAsync(string connStr, string container, string fileName, CancellationToken cancelToken);
    Task UploadAsync(string connStr, string container, string fileName, Stream stream, CancellationToken cancelToken);
}