using System.Text.Json;

namespace Generellem.DataSource;

public class FileSystem : IDocumentSource
{
    static IEnumerable<FileSpec> GetPaths(string configPath = nameof(FileSystem) + ".json")
    {
        using FileStream fileStr = File.OpenRead(configPath);

        IEnumerable<FileSpec>? fileSpec = JsonSerializer.Deserialize<IEnumerable<FileSpec>>(fileStr);

        return fileSpec ?? Enumerable.Empty<FileSpec>();
    }

    /// <summary>
    /// Perform a recursive file search for the entire directory tree from a configured set of paths.
    /// </summary>
    /// <remarks>
    /// This method performs a recursive search through the directory tree rooted at the specified
    /// paths and returns an enumerable collection of all file paths found. It supports Linux, Mac, and Windows.
    /// </remarks>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns>An enumerable collection of <see cref="FileInfo"/> found in the directory tree.</returns>
    public IEnumerable<FileInfo> GetFiles(CancellationToken cancellationToken)
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
                    directories.Enqueue(directory.FullName);

                foreach (var file in directoryInfo.GetFiles())
                    yield return file;
            } 
        }
    }

}
