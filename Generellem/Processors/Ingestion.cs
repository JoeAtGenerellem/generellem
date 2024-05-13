using System.Security.Cryptography;
using System.Text;

using Generellem.Document.DocumentTypes;
using Generellem.DocumentSource;
using Generellem.Embedding;
using Generellem.Repository;
using Generellem.Services;
using Generellem.Services.Azure;
using Generellem.Services.Exceptions;

using Microsoft.Extensions.Logging;

using Polly;

namespace Generellem.Processors;

/// <summary>
/// Ingests documents into the system
/// </summary>
public class Ingestion(
    IAzureSearchService azSearchSvc,
    IDocumentHashRepository docHashRep,
    IDocumentSourceFactory docSourceFact,
    IEmbedding embedding,
    ILogger<Ingestion> logger) 
    : IGenerellemIngestion
{
    readonly ResiliencePipeline pipeline =
        new ResiliencePipelineBuilder()
            .AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is not GenerellemNeedsIngestionException)
            })
            .AddTimeout(TimeSpan.FromSeconds(7))
            .Build();

    /// <summary>
    /// Creates an Azure Search index (if it doesn't already exist), uploads document chunks, and indexes the chunks.
    /// </summary>
    /// <param name="chunks">Mulitple <see cref="TextChunk"/> instances for a document.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public virtual async Task IndexAsync(List<TextChunk> chunks, CancellationToken cancellationToken)
    {
        if (chunks.Count is 0)
            return;

        await pipeline.ExecuteAsync(
            async token => await azSearchSvc.CreateIndexAsync(token),
            cancellationToken);
        await pipeline.ExecuteAsync(
            async token => await azSearchSvc.UploadDocumentsAsync(chunks, token),
            cancellationToken);
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

                documentReferences.Add(doc.DocumentReference);

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

                if (IsDocUnchanged(doc, fullText))
                    continue;

                progress.Report(new($"Ingesting {doc.DocumentReference}", ++count));

                List<TextChunk> chunks = await embedding.EmbedAsync(fullText, doc.DocType, doc.DocumentReference, cancelToken);
                await IndexAsync(chunks, cancelToken);

                if (cancelToken.IsCancellationRequested)
                    break;
            }

            await RemoveDeletedFilesAsync(docSource.Prefix, documentReferences, cancelToken);

            progress.Report(new($"Completed the {docSource.Description} Document Source"));
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
    protected virtual bool IsDocUnchanged(DocumentInfo doc, string fullText)
    {
        string newHash = ComputeSha256Hash(fullText);

        DocumentHash? document = docHashRep.GetDocumentHash(doc.DocumentReference);

        if (document == null)
            try
            {
                docHashRep.Insert(new DocumentHash { DocumentReference = doc.DocumentReference, Hash = newHash });
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
                docHashRep.Update(document, newHash);
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
    protected static string ComputeSha256Hash(string rawData)
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
    /// <param name="docSource">Filters the documentReferences that can be deleted.</param>
    /// <param name="documentReferences">Existing document references.</param>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    public async Task RemoveDeletedFilesAsync(string docSource, List<string> documentReferences, CancellationToken cancellationToken)
    {
        bool doesIndexExist = await azSearchSvc.DoesIndexExistAsync(cancellationToken);
        if (!doesIndexExist)
            return;

        List<TextChunk> chunks = await azSearchSvc.GetDocumentReferencesAsync(docSource, cancellationToken);

        List<string> chunkIdsToDelete = [];
        List<string> chunkDocumentReferencesToDelete = [];

        foreach (TextChunk chunk in chunks)
        {
            if (chunk.DocumentReference is null || documentReferences.Contains(chunk.DocumentReference))
                continue;

            if (chunk?.ID is string chunkID)
                chunkIdsToDelete.Add(chunkID);
            if (chunk?.DocumentReference is string chunkDocumentReference)
                chunkDocumentReferencesToDelete.Add(chunkDocumentReference);
        }

        if (chunkIdsToDelete.Count != 0)
        {
            await pipeline.ExecuteAsync(
                async token => await azSearchSvc.DeleteDocumentReferencesAsync(chunkIdsToDelete, token),
                cancellationToken);
            docHashRep.Delete(chunkDocumentReferencesToDelete);
        }
    }
}
