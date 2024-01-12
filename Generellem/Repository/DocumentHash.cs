using Microsoft.EntityFrameworkCore;

namespace Generellem.Repository;

/// <summary>
/// DB Table for keeping track of file content changes.
/// </summary>
[Index(nameof(DocumentReference))]
public class DocumentHash()
{
    /// <summary>
    /// Unique ID for EF.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique document identifier - used for queries.
    /// </summary>
    public string? DocumentReference { get; set; }

    /// <summary>
    /// Previous file hash for comparing to current file 
    /// hash to see if the file contents are different.
    /// </summary>
    public string? Hash { get; set; }
}
