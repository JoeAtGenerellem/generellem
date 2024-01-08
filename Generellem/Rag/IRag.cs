using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Generellem.Document.DocumentTypes;

namespace Generellem.Rag;

/// <summary>
/// Supports Retrieval Augmented Generation (RAG).
/// </summary>
/// <remarks>
/// The assumption is that all embedding, indexing, and searching happens here.
/// This should be the minimal amount of processing required.
/// </remarks>
public interface IRag
{
    /// <summary>
    /// Takes a raw document, turns it into text, and creates an embedding.
    /// </summary>
    /// <param name="fullText">Document to embed.</param>
    /// <param name="docType">An <see cref="IDocumentType"/> for turning the document into text.</param>
    /// <param name="fileName">Name of file in embedding.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns><see cref="List{T}"/> of <see cref="TextChunk"/>, which holds both the real and embedded content.</returns>
    Task<List<TextChunk>> EmbedAsync(string fullText, IDocumentType docType, string fileName, CancellationToken cancellationToken);

    /// <summary>
    /// Index/Reindex the search engine.
    /// </summary>
    /// <remarks>
    /// Search engines might be different in that they can index individual documents 
    /// or send in multiple documents and then index. Therefore, calling code needs to 
    /// call this because we can't assume when or if indexing should happen.
    /// </remarks>
    /// <param name="chunks">Content and embeddings to upload to the index.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    Task IndexAsync(List<TextChunk> chunks, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes file refs from the index that aren't in the fileRefs argument.
    /// </summary>
    /// <remarks>
    /// The assumption here is that for a given document source, we've identified
    /// all of the files that we can process. However, if there's a file in the
    /// index and not in the document source, the file must have been deleted.
    /// </remarks>
    /// <param name="docSource">Filters the fileRefs that can be deleted.</param>
    /// <param name="docSourceFileRefs">Existing fileRefs.</param>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    Task RemoveDeletedFilesAsync(string docSource, List<string> docSourceFileRefs, CancellationToken cancelToken);

    /// <summary>
    /// Performs Vector Search for chunks matching given text.
    /// </summary>
    /// <param name="text">Text for searching for matches.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns>List of text chunks matching query.</returns>
    Task<List<string>> SearchAsync(string text, CancellationToken cancellationToken);
}
