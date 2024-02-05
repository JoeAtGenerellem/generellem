
namespace Generellem.Processors;

/// <summary>
/// Performs ingestion on configured document sources
/// </summary>
public interface IGenerellemIngestion
{
    /// <summary>
    /// Scans document sources and ingests supported documents.
    /// </summary>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    Task IngestDocumentsAsync(CancellationToken cancelToken);
}