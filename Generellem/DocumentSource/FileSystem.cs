using System.Runtime.CompilerServices;
using System.Text.Json;

using Generellem.Document;
using Generellem.Document.DocumentTypes;

namespace Generellem.DocumentSource;

public class FileSystem : IDocumentSource
{
    readonly IEnumerable<string> DocExtensions = DocumentTypeFactory.GetSupportedDocumentTypes();
    readonly string DocSource = $"{Environment.MachineName}:{nameof(FileSystem)}";
    readonly string[] ExcludedPaths = ["\\bin", "\\obj", ".git", ".vs"];

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

                    yield return new DocumentInfo(DocSource, fileStream, docType, filePath);

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

    public virtual async Task<IEnumerable<FileSpec>> GetPathsAsync(string configPath = nameof(FileSystem) + ".json")
    {
        using FileStream fileStr = File.OpenRead(configPath);

        IEnumerable<FileSpec>? fileSpec = await JsonSerializer.DeserializeAsync<IEnumerable<FileSpec>>(fileStr);

        return fileSpec ?? Enumerable.Empty<FileSpec>();
    }

    public virtual bool IsPathExcluded(string directoryPath)
    {
        foreach (string xPath in ExcludedPaths)
            if (directoryPath.Contains(xPath))
                return true;

        return false;
    }
}
