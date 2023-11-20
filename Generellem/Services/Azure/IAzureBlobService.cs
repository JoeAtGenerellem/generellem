namespace Generellem.Services.Azure;

public interface IAzureBlobService
{
    Task DeleteAsync(string fileName);
    Task<Stream> DownloadAsync(string fileName);
    Task UploadAsync(string fileName, Stream stream);
}