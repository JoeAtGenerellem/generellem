using Generellem.Services;

using System.Text;
using System.Text.Json;

namespace Generellem.DocumentSource;

public class OneDriveFilePathProvider(IGenerellemFiles gemFiles) : IPathProvider
{
    /// <summary>
    /// Extracts file paths from the config file.
    /// </summary>
    /// <param name="configPath">Location of the config file.</param>
    /// <returns>Enumerable of <see cref="OneDriveSpec"/>.</returns>
    public virtual async Task<IEnumerable<PathSpec>> GetPathsAsync(string configPath = nameof(OneDriveFileSystem) + ".json")
    {
        configPath = gemFiles.GetAppDataPath(configPath);

        if (!File.Exists(configPath))
            using (FileStream specWriter = File.OpenWrite(configPath))
                await specWriter.WriteAsync(new ReadOnlyMemory<byte>(Encoding.Default.GetBytes("[]")));

        using FileStream specReader = File.OpenRead(configPath);

        IEnumerable<PathSpec>? fileSpec = await JsonSerializer.DeserializeAsync<IEnumerable<PathSpec>>(specReader);

        return fileSpec ?? Enumerable.Empty<PathSpec>();
    }

    /// <summary>
    /// Writes file paths to the config file.
    /// </summary>
    /// <param name="fileSpecs">Enumerable of <see cref="PathSpec"/>.</param>
    /// <param name="configPath">Location of the config file.</param>
    public virtual async ValueTask WritePathsAsync(IEnumerable<PathSpec> fileSpecs, string configPath = nameof(OneDriveFileSystem) + ".json")
    {
        configPath = gemFiles.GetAppDataPath(configPath);

        File.Delete(configPath);

        using FileStream specWriter = File.OpenWrite(configPath);

        string specJson = JsonSerializer.Serialize(fileSpecs, new JsonSerializerOptions() { WriteIndented = true });
        byte[] specBytes = Encoding.Default.GetBytes(specJson);
        ReadOnlyMemory<byte> specMem = new(specBytes);

        await specWriter.WriteAsync(specMem);
    }

    /// <summary>
    /// Helps avoid paths we don't want to follow.
    /// </summary>
    /// <param name="directoryPath">Path to check.</param>
    /// <returns>true if excluded, false if not.</returns>
    public virtual bool IsPathExcluded(string directoryPath)
    {
        return false;
    }
}
