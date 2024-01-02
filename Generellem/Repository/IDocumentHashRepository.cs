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
/// <param name="ctx"><see cref="GenerellemContext"/></param>
public interface IDocumentHashRepository
{
    /// <summary>
    /// Queries the document hash based on fileRef.
    /// </summary>
    /// <param name="fileRef">Unique name for file.</param>
    /// <returns><see cref="DocumentHash"/> or null if not found.</returns>
    DocumentHash? GetDocumentHash(string fileRef);

    /// <summary>
    /// This is the first time we've scanned a document, so add a new record.
    /// </summary>
    /// <param name="docHash"><see cref="DocumentHash"/> to add.</param>
    void Insert(DocumentHash docHash);

    /// <summary>
    /// The document has changed and we need to update it's hash value.
    /// </summary>
    /// <param name="docHash">The existing document hash.</param>
    /// <param name="hash">The new hash value.</param>
    void Update(DocumentHash docHash, string hash);
}
