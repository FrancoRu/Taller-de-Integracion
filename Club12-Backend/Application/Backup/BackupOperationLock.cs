using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Backup;

/// <summary>
/// Process-wide single-flight guard for backup/restore write operations.
/// Wraps a SemaphoreSlim(1,1): WaitAsync(TimeSpan.Zero) either
/// acquires the lock immediately or returns false without blocking,
/// so a concurrent caller (manual endpoint vs. scheduled job, or two
/// overlapping manual calls) is told "busy" instead of queueing. Registered
/// as a singleton so both DatabaseBackupHostedService (scheduled)
/// and BackupController (manual, via the scoped
/// IBackupOperationsService) serialize against the same instance.
/// Valid only for a single-process deployment — see design.md's
/// "In-process SemaphoreSlim singleton" decision.
/// </summary>
public sealed class BackupOperationLock
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Attempts to acquire the lock, waiting up to timeout. Callers
    /// use TimeSpan.Zero to fail fast ("busy") rather than queue.
    /// </summary>
    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        return _semaphore.WaitAsync(timeout, ct);
    }

    /// <summary>
    /// Releases the lock. Must be called exactly once for every successful
    /// WaitAsync that returned true.
    /// </summary>
    public void Release()
    {
        _semaphore.Release();
    }
}
