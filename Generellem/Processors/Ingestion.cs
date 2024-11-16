using Generellem.Document.DocumentTypes;
using Generellem.DocumentSource;
using Generellem.Embedding;
using Generellem.Repository;
using Generellem.Services;

using Microsoft.Extensions.Logging;

using Polly;

using System.Security.Cryptography;
using System.Text;

namespace Generellem.Processors;

/// <summary>
/// Ingests documents into the system
/// </summary>
public class Ingestion(
    IDocumentHashRepository docHashRep,
    IDocumentSourceFactory docSourceFact,
    IEmbedding embedding,
    ILogger<Ingestion> logger,
    ISearchService azSearchSvc)
    : IGenerellemIngestion
{
#if DEBUG
    readonly ResiliencePipeline pipeline =
    new ResiliencePipelineBuilder()
        .AddRetry(new()
        {
            ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => false)
        })
        .AddTimeout(TimeSpan.FromSeconds(1))
        .Build();
#else
    readonly ResiliencePipeline pipeline =
        new ResiliencePipelineBuilder()
            .AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is not GenerellemNeedsIngestionException)
            })
            .AddTimeout(TimeSpan.FromSeconds(7))
            .Build();
#endif

    /// <summary>
    /// Creates an Azure Search index (if it doesn't already exist), uploads document chunks, and indexes the chunks.
    /// </summary>
    /// <param name="doc"><see cref="DocumentInfo"/></param>
    /// <param name="docSrc"><see cref="IDocumentSource"/></param>
    /// <param name="fullText">Plaintext of document.</param>
    /// <param name="progress">Reports progress.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public virtual async Task IndexAsync(DocumentInfo doc, string fullText, IDocumentSource docSrc, IProgress<IngestionProgress> progress, CancellationToken cancellationToken)
    {
        if (doc?.DocType is null)
            return;

        List<TextChunk> chunks = await embedding.EmbedAsync(fullText, doc.DocType, doc.DocumentReference, progress, cancellationToken);
        await azSearchSvc.CreateIndexAsync(cancellationToken);
        await azSearchSvc.UploadDocumentsAsync(chunks, cancellationToken);
    }

    /// <summary>
    /// Recursive search of documents from specified document sources
    /// </summary>
    /// <param name="progress">Lets the caller receive progress updates.</param>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    public virtual async Task IngestDocumentsAsync(IProgress<IngestionProgress> progress,CancellationToken cancelToken)
    {
        int count = 0;

        IEnumerable<IDocumentSource> docSources = docSourceFact.GetDocumentSources();

        progress.Report(new($"Processing document sources..."));

        foreach (IDocumentSource docSource in docSources)
        {
            progress.Report(new($"Starting on the {docSource.Description} Document Source"));

            List<string> documentReferences = [];

            await foreach (DocumentInfo doc in docSource.GetDocumentsAsync(cancelToken))
            {
                ArgumentNullException.ThrowIfNull(doc);
                ArgumentNullException.ThrowIfNull(doc.DocStream);
                ArgumentNullException.ThrowIfNull(doc.DocType);
                ArgumentException.ThrowIfNullOrEmpty(doc.DocPath);
                ArgumentException.ThrowIfNullOrEmpty(doc.DocumentReference);

                 if (doc.DocType.GetType() == typeof(Unknown))
                    continue;

                string fullText;
                try
                {
                    fullText = await doc.DocType.GetTextAsync(doc.DocStream, doc.DocPath);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(GenerellemLogEvents.DocumentError, ex, "Unable to process file: {FilePath}", doc.DocPath);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(fullText))
                {
                    //logger.LogInformation($"{DateTime.Now} Added {doc.DocumentReference} to {nameof(documentReferences)}");
                    documentReferences.Add(doc.DocumentReference);
                }

                if (await IsDocUnchangedAsync(doc, fullText))
                {
                    //logger.LogInformation($"{DateTime.Now} {doc.DocumentReference} is unchanged.");
                    continue;
                }

                progress.Report(new($"Ingesting {doc.DocumentReference}", ++count));

                await IndexAsync(doc, fullText, docSource, progress, cancelToken);

                //logger.LogInformation($"{DateTime.Now} {doc.DocumentReference} added to index.");

                if (cancelToken.IsCancellationRequested)
                    break;
            }

            await RemoveDeletedFilesAsync(docSource.Reference, documentReferences, cancelToken);

            progress.Report(new($"Completed the {docSource.Description} Document Source"));
            progress.Report(new($""));
        }

        progress.Report(new($"Ingestion is complete. Total documents processed for this job: {count}"));
    }

    /// <summary>
    /// Compares hash of new document vs. hash of previous document to determine if anything changed.
    /// </summary>
    /// <remarks>
    /// This is an optimization to ensure we don't update documents that haven't changed.
    /// If the document doesn't exist in the local DB, it's new and we insert it.
    /// If the hashes are different, we insert the document into the local DB.
    /// </remarks>
    /// <param name="doc"><see cref="DocumentInfo"/> metadata of document.</param>
    /// <param name="fullText">Document text.</param>
    /// <returns>True if the current and previous hashes match.</returns>
    public virtual async Task<bool> IsDocUnchangedAsync(DocumentInfo doc, string fullText)
    {
        string newHash = ComputeSha256Hash(fullText);

        DocumentHash? document = await docHashRep.GetDocumentHashAsync(doc.DocumentReference);

        if (document is not null && string.IsNullOrWhiteSpace(fullText))
            try
            {
                await docHashRep.DeleteAsync([doc.DocumentReference]);
                logger.LogInformation($"{DateTime.Now} {doc.DocumentReference} deleted from hash table.");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    "Unable to delete doc hash - {DocumentReference}, {DocumentHash}, {Exception}",
                    doc.DocumentReference, newHash, ex);
                return true;
            }
        else if (document?.Hash == null)
            try
            {
                await docHashRep.InsertAsync(new DocumentHash { DocumentReference = doc.DocumentReference, Hash = newHash });
                logger.LogInformation($"{DateTime.Now} {doc.DocumentReference} inserted into hash table.");
            }
            catch (Exception ex)
            {
                logger.LogError(
                    "Unable to insert doc hash - {DocumentReference}, {DocumentHash}, {Exception}",
                    doc.DocumentReference, newHash, ex);
            }
        else if (document.Hash != newHash)
            try
            {
                await docHashRep.UpdateAsync(document, newHash);
                logger.LogInformation($"{DateTime.Now} {doc.DocumentReference} updated in hash table.");
            }
            catch (Exception ex)
            {
                logger.LogError(
                    "Unable to update doc hash - {DocumentReference}, {DocumentHash}, {Exception}",
                    doc.DocumentReference, newHash, ex);
            }
        else
            return true;

        return false;
    }

    /// <summary>
    /// We're using a SHA256 hash to compare documents.
    /// </summary>
    /// <param name="rawData">Document Text.</param>
    /// <returns>Hash of the document text.</returns>
    public static string ComputeSha256Hash(string rawData)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));

        StringBuilder sb = new();

        for (int i = 0; i < bytes.Length; i++)
            sb.Append(bytes[i].ToString("x2"));

        return sb.ToString();
    }

    /// <summary>
    /// Deletes file refs from the index and local DB that aren't in the documentReferences argument.
    /// </summary>
    /// <remarks>
    /// The assumption here is that for a given document source, we've identified
    /// all of the files that we can process. However, if there's a file in the
    /// index and not in the document source, the file must have been deleted.
    /// </remarks>
    /// <param name="docSourcePrefix">Filters the documentReferences that can be deleted.</param>
    /// <param name="documentReferences">Existing document references.</param>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    public async Task RemoveDeletedFilesAsync(string docSourcePrefix, List<string> documentReferences, CancellationToken cancellationToken)
    {
        List<TextChunk> chunks = await azSearchSvc.GetDocumentReferencesAsync(docSourcePrefix, cancellationToken);

        List<string> chunkIdsToDelete = [];
        List<string> chunkDocumentReferencesToDelete = [];

        // delete in index but not in hashes
        foreach (TextChunk chunk in chunks)
        {
            if (chunk.DocumentReference is null || documentReferences.Contains(chunk.DocumentReference))
            {
                //logger.LogInformation($"{DateTime.Now} {chunk.DocumentReference} either doesn't exist or is already known.");
                continue;
            }

            if (chunk?.ID is string chunkID)
            {
                chunkIdsToDelete.Add(chunkID);
                //logger.LogInformation($"{DateTime.Now} {chunk.DocumentReference} has an ID.");
            }
            else
            {
                //logger.LogInformation($"{DateTime.Now} {chunk?.DocumentReference} does not have an ID.");
            }

            if (chunk?.DocumentReference is string chunkDocumentReference)
            {
                chunkDocumentReferencesToDelete.Add(chunkDocumentReference);
                //logger.LogInformation($"{DateTime.Now} {chunk.DocumentReference} has a document reference.");
            }
            else
            {
                //logger.LogInformation($"{DateTime.Now} {chunk?.DocumentReference} doesn't have a document reference.");
            }
        }

        List<string> indexReferences =
            (from chunk in chunks
             select chunk.DocumentReference)
            .Distinct()
            .ToList();

        // delete in hashes but not in index
        foreach (string docRef in documentReferences)
            if (!indexReferences.Contains(docRef))
            {
                chunkDocumentReferencesToDelete.Add(docRef);
                //logger.LogInformation($"{DateTime.Now} the index contains {docRef}.");
            }
            else
            {
                //logger.LogInformation($"{DateTime.Now} the index does not contain {docRef}.");
            }

        if (chunkIdsToDelete.Count != 0)
        {
            await pipeline.ExecuteAsync(
                async token => await azSearchSvc.DeleteDocumentReferencesAsync(chunkIdsToDelete, token),
                cancellationToken);
            //logger.LogInformation($"{DateTime.Now} Deleted {chunkIdsToDelete.Count} chunks from index.");
        }
        else
        {
            //logger.LogInformation($"{DateTime.Now} No chunks to delete.");
        }
        
        if (chunkDocumentReferencesToDelete.Count != 0)
        {
            await docHashRep.DeleteAsync(chunkDocumentReferencesToDelete);
            //logger.LogInformation($"{DateTime.Now} Deleted {chunkDocumentReferencesToDelete.Count} document references from  hash.");
        }
        else
        {
            //logger.LogInformation($"{DateTime.Now} No document references to delete.");
        }

    }
}
