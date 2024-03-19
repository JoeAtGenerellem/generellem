
namespace Generellem.Processors;

/// <summary>
/// Performs ingestion on configured document sources
/// </summary>
public interface IGenerellemIngestion
{
    /// <summary>
    /// Recursive search of documents from specified document sources
    /// </summary>
    /// <param name="progress">Lets the caller receive progress updates.</param>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    Task IngestDocumentsAsync(IProgress<IngestionProgress> progress, CancellationToken cancelToken);
}