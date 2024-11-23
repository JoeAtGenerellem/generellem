
using Generellem.DocumentSource;

namespace Generellem.Processors;

/// <summary>
/// Performs ingestion on configured document sources
/// </summary>
public interface IGenerellemIngestion
{
    /// <summary>
    /// Creates an Azure Search index (if it doesn't already exist), uploads document chunks, and indexes the chunks.
    /// </summary>
    /// <param name="doc"><see cref="DocumentInfo"/></param>
    /// <param name="fullText">Plaintext of document.</param>
    /// <param name="docSrc"><see cref="IDocumentSource"/></param>
    /// <param name="progress">Reports progress.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    Task InsertOrUpdateDocumentAsync(DocumentInfo doc, string fullText, IDocumentSource docSrc, IProgress<IngestionProgress> progress, CancellationToken cancellationToken);

    /// <summary>
    /// Recursive search of documents from specified document sources
    /// </summary>
    /// <param name="progress">Lets the caller receive progress updates.</param>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    /// <param name="enableFileTracking">
    /// Keep track of changes. Useful for full file system or website scanning 
    /// to know which files were added, modified, or deleted. Not used in other
    /// systems that provide real-time notifications, via webhook.
    /// </param>
    Task IngestDocumentsAsync(IProgress<IngestionProgress> progress, CancellationToken cancelToken);
    
    /// <summary>
    /// Removes document references from the search DB.
    /// </summary>
    /// <param name="chunkIdsToDelete">IDs of chunks to delete.</param>
    /// <param name="docSrc">Indicates which data source to delete from.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    Task RemoveFromSearchAsync(List<string> chunkIdsToDelete, IDocumentSource docSrc, CancellationToken cancellationToken);
}