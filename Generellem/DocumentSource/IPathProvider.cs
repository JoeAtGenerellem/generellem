namespace Generellem.DocumentSource;

public interface IPathProvider
{
    Task<IEnumerable<PathSpec>> GetPathsAsync(string configPath);
    bool IsPathExcluded(string directoryPath);
    ValueTask WritePathsAsync(IEnumerable<PathSpec> fileSpecs, string configPath);
}