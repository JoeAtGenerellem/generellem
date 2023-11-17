using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Generellem.Document.DocumentTypes;

namespace Generellem.RAG;

/// <summary>
/// Supports Retrieval Augmented Generation
/// </summary>
/// <remarks>
/// The assumption is that all embedding, indexing, and searching happens here.
/// This should be the minimal amount of processing required.
/// </remarks>
public interface IRag
{
    /// <summary>
    /// Takes a raw document, turns it into text, and creates an embedding
    /// </summary>
    /// <param name="documentStream">Document to embed</param>
    /// <param name="docType">An <see cref="IDocumentType"/> for turning the document into text</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    Task Embedsync(Stream documentStream, IDocumentType docType, CancellationToken cancellationToken);

    /// <summary>
    /// Index/Reindex the search engine
    /// </summary>
    /// <remarks>
    /// Search engines might be different in that they can index individual documents 
    /// or send in multiple documents and then index. Therefore, calling code needs to 
    /// call this because we can't assume when or if indexing should happen.
    /// </remarks>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    Task IndexAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets a list of documents matching search criteria.
    /// </summary>
    /// <param name="text">Search criteria</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns>List of documents</returns>
    Task<List<string>> SearchAsync(string text, CancellationToken cancellationToken);
}
