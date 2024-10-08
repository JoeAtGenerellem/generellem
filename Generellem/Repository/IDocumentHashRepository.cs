﻿
namespace Generellem.Repository;

/// <summary>
/// DB Operations on Document Hashes
/// </summary>
/// <remarks>
/// A document hash is the hashed representation of a document.
/// It's purpose is to compare a current document with the
/// previous version of the document to see if the contents
/// of the document changed since we last scanned it.
/// </remarks>
public interface IDocumentHashRepository
{
    /// <summary>
    /// Delete the <see cref="DocumentHash"/>.
    /// </summary>
    /// <param name="documentReferences">Unique DocumentReferences for the <see cref="DocumentHash"/>'s to delete.</param>
    Task DeleteAsync(List<string> documentReferences);

    /// <summary>
    /// Queries a <see cref="DocumentHash"/> based on documentReference.
    /// </summary>
    /// <param name="documentReference">Unique name for file.</param>
    /// <returns><see cref="DocumentHash"/> or null if not found.</returns>
    Task<DocumentHash?> GetDocumentHashAsync(string documentReference);

    /// <summary>
    /// Queries for <see cref="DocumentHash"/>'s based on documentReferences.
    /// </summary>
    /// <param name="documentReferences">Unique names for files.</param>
    /// <returns><see cref="List{T}"/> of <see cref="DocumentHash"/>'s.</returns>
    Task<List<DocumentHash>> GetDocumentHashesAsync(List<string> documentReferences);

    /// <summary>
    /// This is the first time we've scanned a document, so add a new record.
    /// </summary>
    /// <param name="docHash"><see cref="DocumentHash"/> to add.</param>
    Task InsertAsync(DocumentHash docHash);

    /// <summary>
    /// The document has changed and we need to update it's hash value.
    /// </summary>
    /// <param name="docHash">The existing <see cref="DocumentHash"/>.</param>
    /// <param name="hash">The new hash value.</param>
    Task UpdateAsync(DocumentHash docHash, string hash);
}
