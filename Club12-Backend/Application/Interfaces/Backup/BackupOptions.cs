namespace Application.Interfaces.Backup;

/// <summary>
/// Non-secret backup configuration bound from the Backup configuration
/// section. Secrets (DB connection, storage credentials) are read separately
/// from ConnectionStrings/SupaBase, never stored here.
/// </summary>
public sealed class BackupOptions
{
    public bool Enabled { get; set; }
    public int IntervalHours { get; set; } = 24;
    public int RetentionCount { get; set; } = 7;
    public string StorageTarget { get; set; } = BackupStorageTargets.Local;
    public string LocalStoragePath { get; set; } = "backups";
    public string PgDumpPath { get; set; } = "pg_dump";
}
