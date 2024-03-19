using System.Runtime.CompilerServices;

namespace Generellem.DocumentSource;
public interface IFileSystem
{
    string Description { get; set; }
    string Prefix { get; init; }

    IAsyncEnumerable<DocumentInfo> GetDocumentsAsync(CancellationToken cancelToken);
    Task<IEnumerable<FileSpec>> GetPathsAsync(string configPath = "FileSystem.json");
    bool IsPathExcluded(string directoryPath);
    ValueTask WritePathsAsync(IEnumerable<FileSpec> fileSpecs, string configPath = "FileSystem.json");
}