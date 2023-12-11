using System.Text.Json;

namespace Generellem.DocumentSource;

public class FileSystem : IDocumentSource
{
    readonly string[] ExcludedPaths = new string[] { "\\bin", "\\obj", ".git", ".vs" };

    /// <summary>
    /// Perform a recursive file search for the entire directory tree from a configured set of paths.
    /// </summary>
    /// <remarks>
    /// This method performs a recursive search through the directory tree rooted at the specified
    /// paths and returns an enumerable collection of all file paths found. It supports Linux, Mac, and Windows.
    /// </remarks>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    /// <returns>An enumerable collection of <see cref="FileInfo"/> found in the directory tree.</returns>
    public virtual IEnumerable<FileInfo> GetFiles(CancellationToken cancelToken)
    {
        IEnumerable<FileSpec> fileSpecs = GetPaths();

        foreach (var spec in fileSpecs)
        {
            if (spec?.Path is not string path) continue;

            var directories = new Queue<string>();
            directories.Enqueue(path);

            while (directories.Any())
            {
                var currentDirectory = directories.Dequeue();
                var directoryInfo = new DirectoryInfo(currentDirectory);

                foreach (var directory in directoryInfo.GetDirectories())
                    if (!IsPathExcluded(directory.FullName))
                        directories.Enqueue(directory.FullName);

                foreach (var file in directoryInfo.GetFiles())
                    yield return file;

                if (cancelToken.IsCancellationRequested)
                    break;
            }

            if (cancelToken.IsCancellationRequested)
                break;
        }
    }

    public virtual IEnumerable<FileSpec> GetPaths(string configPath = nameof(FileSystem) + ".json")
    {
        using FileStream fileStr = File.OpenRead(configPath);

        IEnumerable<FileSpec>? fileSpec = JsonSerializer.Deserialize<IEnumerable<FileSpec>>(fileStr);

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
