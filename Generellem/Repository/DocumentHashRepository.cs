using Microsoft.EntityFrameworkCore;

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
    /// <param name="documentReferences">Unique DocumentReferences for the <see cref="DocumentHash"/>'s to delete.</param>
    public async Task DeleteAsync(List<string> documentReferences)
    {
        List<DocumentHash> docHashes = await GetDocumentHashesAsync(documentReferences);

        ctx.DocumentHashes.RemoveRange(docHashes);
        await ctx.SaveChangesAsync();
    }

    /// <summary>
    /// Queries a <see cref="DocumentHash"/> based on DocumentReference.
    /// </summary>
    /// <param name="documentReference">Unique name for file.</param>
    /// <returns><see cref="DocumentHash"/> or null if not found.</returns>
    public async Task<DocumentHash?> GetDocumentHashAsync(string documentReference) =>
        await
        (from docHash in ctx.DocumentHashes
         where docHash.DocumentReference == documentReference
         select docHash)
        .SingleOrDefaultAsync();

    /// <summary>
    /// Queries for <see cref="DocumentHash"/>'s based on DocumentReferences.
    /// </summary>
    /// <param name="documentReferences">Unique names for files.</param>
    /// <returns><see cref="List{T}"/> of <see cref="DocumentHash"/>'s.</returns>
    public async Task<List<DocumentHash>> GetDocumentHashesAsync(List<string> documentReferences) =>
        await
        (from docHash in ctx.DocumentHashes
         where documentReferences.Contains(docHash.DocumentReference ?? string.Empty)
         select docHash)
        .ToListAsync();

    /// <summary>
    /// This is the first time we've scanned a document, so add a new record.
    /// </summary>
    /// <param name="docHash"><see cref="DocumentHash"/> to add.</param>
    public async Task InsertAsync(DocumentHash docHash)
    {
        ctx.DocumentHashes.Add(docHash);
        await ctx.SaveChangesAsync();
    }

    /// <summary>
    /// The document has changed and we need to update it's hash value.
    /// </summary>
    /// <param name="docHash">The existing <see cref="DocumentHash"/>.</param>
    /// <param name="hash">The new hash value.</param>
    public async Task UpdateAsync(DocumentHash docHash, string hash)
    {
        docHash.Hash = hash;
        await ctx.SaveChangesAsync();
    }
}
