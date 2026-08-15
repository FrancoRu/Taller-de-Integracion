using System;

namespace Application.Interfaces.Backup;

/// <summary>
/// Metadata describing a single stored backup dump: its name (as stored —
/// e.g. object key or file name) and its creation timestamp.
/// </summary>
public sealed record BackupFile(string Name, DateTimeOffset Timestamp);

/// <summary>
/// Result of running an external process via IProcessRunner.
/// A non-zero ExitCode (including the sentinel value the
/// ProcessRunner adapter returns when the process could not even be
/// started, e.g. a missing binary) signals failure; callers must not treat
/// StdOut as valid output unless ExitCode is 0.
/// </summary>
public sealed record ProcessResult(int ExitCode, string StdOut, string StdErr);

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
    public string StorageTarget { get; set; } = "Local";
    public string LocalStoragePath { get; set; } = "backups";
    public string PgDumpPath { get; set; } = "pg_dump";
}

/// <summary>
/// Raised when a backup step (dump creation, storage, pruning) fails in an
/// expected/handled way — e.g. pg_dump exits non-zero or the binary is
/// missing. Callers (the scheduled hosted service, added in a later work
/// unit) are expected to catch this, log it, and keep running; it must never
/// be allowed to propagate as an unhandled exception that crashes the host.
/// </summary>
public sealed class BackupExecutionException : Exception
{
    public BackupExecutionException(string message) : base(message)
    {
    }

    public BackupExecutionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
