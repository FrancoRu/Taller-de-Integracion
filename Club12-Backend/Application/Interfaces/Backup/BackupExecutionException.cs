using System;

namespace Application.Interfaces.Backup;

/// <summary>
/// Raised when a backup step (dump creation, storage, pruning) fails in an
/// expected/handled way — e.g. pg_dump exits non-zero or the binary is
/// missing. Callers (the scheduled hosted service) are expected to catch
/// this, log it, and keep running; it must never be allowed to propagate as
/// an unhandled exception that crashes the host.
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
