namespace Generellem.DataSource;

public interface IDocumentSource
{
    IEnumerable<FileInfo> GetFiles(CancellationToken cancellationToken);
}
