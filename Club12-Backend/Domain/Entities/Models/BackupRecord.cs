using Domain.Enums;

namespace Domain.Entities.Models;

/// <summary>
/// Durable, queryable per-backup record, the source of truth for the admin backup listing.
/// </summary>
public class BackupRecord : EntityBase
{
    /// <summary>
    /// Where the dump is stored, as an object key or file path understood by the IBackupStorage adapter that persisted it.
    /// </summary>
    public required string StoragePath { get; set; }

    /// <summary>
    /// Size, in bytes, of the stored dump.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Whether this backup was triggered manually by an Admin or created by the scheduled backup job.
    /// </summary>
    public required BackupOrigin Origin { get; set; }
}
