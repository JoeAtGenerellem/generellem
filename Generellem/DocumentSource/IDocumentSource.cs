namespace Generellem.DocumentSource;

public interface IDocumentSource
{
    IEnumerable<FileInfo> GetFiles(CancellationToken cancellationToken);
}
