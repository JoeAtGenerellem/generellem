using Generellem.Services;

namespace Generellem.DocumentSource;

public class PathProviderFactory(IGenerellemFiles gemFiles) : IPathProviderFactory
{
    protected virtual Dictionary<Type, IPathProvider> PathProviders { get; set; } =
        new()
        {
            [typeof(OneDriveFileSystem)] = new OneDriveFilePathProvider(gemFiles),
            [typeof(FileSystem)] = new FilePathProvider(gemFiles),
            [typeof(Website)] = new FilePathProvider(gemFiles)
        };

    public virtual IPathProvider Create(IDocumentSource docSource)
    {
        ArgumentNullException.ThrowIfNull(docSource, $"{nameof(docSource)} is a require parameter.");

        return PathProviders[docSource.GetType()];
    }
}
