using System.Runtime.CompilerServices;
using System.Text.Json;

using Generellem.Document;
using Generellem.Document.DocumentTypes;

namespace Generellem.DocumentSource;

/// <summary>
/// Supports ingesting documents from a computer file system
/// </summary>
public class FileSystem : IDocumentSource
{
    public string Prefix { get; init; } = $"{Environment.MachineName}:{nameof(FileSystem)}";

    readonly IEnumerable<string> DocExtensions = DocumentTypeFactory.GetSupportedDocumentTypes();
    readonly string[] ExcludedPaths = ["\\bin", "\\obj", ".git", ".vs"];

    /// <summary>
    /// Based on the config file, scan files.
    /// </summary>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    /// <returns>Enumerable of <see cref="DocumentInfo"/>.</returns>
    public async IAsyncEnumerable<DocumentInfo> GetDocumentsAsync([EnumeratorCancellation] CancellationToken cancelToken)
    {
        IEnumerable<FileSpec> fileSpecs = await GetPathsAsync();

        foreach (var spec in fileSpecs)
        {
            if (spec?.Path is not string path)
                continue;

            Queue<string> directories = new();
            directories.Enqueue(path);

            while (directories.Count is not 0)
            {
                string currentDirectory = directories.Dequeue();
                DirectoryInfo directoryInfo = new(currentDirectory);

                foreach (var directory in directoryInfo.GetDirectories())
                    if (!IsPathExcluded(directory.FullName))
                        directories.Enqueue(directory.FullName);

                foreach (FileInfo file in directoryInfo.GetFiles())
                {
                    string filePath = file.FullName;
                    string fileName = Path.GetFileName(filePath);

                    IDocumentType docType = DocumentTypeFactory.Create(fileName);
                    Stream fileStream = File.OpenRead(filePath);

                    yield return new DocumentInfo(Prefix, fileStream, docType, filePath);

                    if (cancelToken.IsCancellationRequested)
                        break;
                }

                if (cancelToken.IsCancellationRequested)
                    break;
            }

            if (cancelToken.IsCancellationRequested)
                break;
        }
    }

    /// <summary>
    /// Extracts file paths from the config file.
    /// </summary>
    /// <param name="configPath">Location of the config file.</param>
    /// <returns>Enumerable of <see cref="FileSpec"/>.</returns>
    public virtual async Task<IEnumerable<FileSpec>> GetPathsAsync(string configPath = nameof(FileSystem) + ".json")
    {
        using FileStream fileStr = File.OpenRead(configPath);

        IEnumerable<FileSpec>? fileSpec = await JsonSerializer.DeserializeAsync<IEnumerable<FileSpec>>(fileStr);

        return fileSpec ?? Enumerable.Empty<FileSpec>();
    }

    /// <summary>
    /// Helps avoid paths we don't want to follow.
    /// </summary>
    /// <param name="directoryPath">Path to check.</param>
    /// <returns>true if excluded, false if not.</returns>
    public virtual bool IsPathExcluded(string directoryPath)
    {
        foreach (string xPath in ExcludedPaths)
            if (directoryPath.Contains(xPath))
                return true;

        return false;
    }
}
