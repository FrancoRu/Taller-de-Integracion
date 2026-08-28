using Application.Interfaces.Backup;

namespace API.Tests.Backup.Fakes;

/// <summary>
/// Test double for IDatabaseRestoreService. No real psql binary
/// involved — records the dump file path it was invoked with (so tests can
/// assert the temp file cleanup happens after the call) and can be
/// configured to throw to simulate a failed restore (spec
/// database-restore#Restore-Failure-Is-Logged-and-Isolated).
/// </summary>
public sealed class FakeDatabaseRestoreService : IDatabaseRestoreService
{
    private int _callCount;

    public int CallCount => _callCount;

    public string? CapturedDumpFilePath { get; private set; }

    /// <summary>
    /// When set, RestoreAsync throws this instead of succeeding.
    /// </summary>
    public Exception? ExceptionToThrow { get; set; }

    public Task RestoreAsync(string dumpFilePath, CancellationToken ct = default)
    {
        Interlocked.Increment(ref _callCount);
        CapturedDumpFilePath = dumpFilePath;

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        return Task.CompletedTask;
    }
}
