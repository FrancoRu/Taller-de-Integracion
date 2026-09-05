using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Backup;

/// <summary>
/// Process-wide single-flight guard for backup and restore write operations.
/// </summary>
public sealed class BackupOperationLock
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Attempts to acquire the lock, waiting up to timeout; callers pass TimeSpan.Zero to fail fast.
    /// </summary>
    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        return _semaphore.WaitAsync(timeout, ct);
    }

    /// <summary>
    /// Releases the lock; must be called exactly once for every successful WaitAsync that returned true.
    /// </summary>
    public void Release()
    {
        _semaphore.Release();
    }
}
