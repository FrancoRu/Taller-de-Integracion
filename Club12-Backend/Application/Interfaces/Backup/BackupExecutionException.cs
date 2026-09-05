using System;

namespace Application.Interfaces.Backup;

/// <summary>
/// Raised when a backup step fails in an expected, handled way that callers must catch and log instead of letting it crash the host.
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
