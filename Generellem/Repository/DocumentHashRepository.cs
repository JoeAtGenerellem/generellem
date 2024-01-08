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
public class DocumentHashRepository(GenerellemContext ctx) : IDocumentHashRepository
{
    /// <summary>
    /// Delete the <see cref="DocumentHash"/>.
    /// </summary>
    /// <param name="fileRefs">Unique FileRefs for the <see cref="DocumentHash"/>'s to delete.</param>
    public void Delete(List<string> fileRefs)
    {
        List<DocumentHash> docHashes = GetDocumentHashes(fileRefs);

        ctx.DocumentHashes.RemoveRange(docHashes);
        ctx.SaveChanges();
    }

    /// <summary>
    /// Queries a <see cref="DocumentHash"/> based on fileRef.
    /// </summary>
    /// <param name="fileRef">Unique name for file.</param>
    /// <returns><see cref="DocumentHash"/> or null if not found.</returns>
    public DocumentHash? GetDocumentHash(string fileRef) =>
        (from docHash in ctx.DocumentHashes
         where docHash.FileRef == fileRef
         select docHash)
        .SingleOrDefault();

    /// <summary>
    /// Queries for <see cref="DocumentHash"/>'s based on fileRefs.
    /// </summary>
    /// <param name="fileRefs">Unique names for files.</param>
    /// <returns><see cref="List{T}"/> of <see cref="DocumentHash"/>'s.</returns>
    public List<DocumentHash> GetDocumentHashes(List<string> fileRefs) =>
        (from docHash in ctx.DocumentHashes
         where fileRefs.Contains(docHash.FileRef ?? string.Empty)
         select docHash)
        .ToList();

    /// <summary>
    /// This is the first time we've scanned a document, so add a new record.
    /// </summary>
    /// <param name="docHash"><see cref="DocumentHash"/> to add.</param>
    public void Insert(DocumentHash docHash)
    {
        ctx.DocumentHashes.Add(docHash);
        ctx.SaveChanges();
    }

    /// <summary>
    /// The document has changed and we need to update it's hash value.
    /// </summary>
    /// <param name="docHash">The existing <see cref="DocumentHash"/>.</param>
    /// <param name="hash">The new hash value.</param>
    public void Update(DocumentHash docHash, string hash)
    {
        docHash.Hash = hash;
        ctx.SaveChanges();
    }
}
