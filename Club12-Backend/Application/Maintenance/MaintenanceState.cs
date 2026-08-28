using Application.Interfaces.Maintenance;

using System;
using System.Threading;

namespace Application.Maintenance;

/// <summary>
/// Default <see cref="IMaintenanceState"/>: a single-slot, thread-safe lock
/// guarded by an <see cref="Interlocked"/> compare-exchange, so at most one
/// backup/restore can hold it at a time. Pure in-process state — no I/O — so
/// it lives in the Application layer and is trivially unit-testable.
/// </summary>
/// <remarks>
/// Deliberately in-memory and single-process (HU-92's documented limitation:
/// a lock is scoped to this host instance; it is not a distributed lock).
/// </remarks>
public sealed class MaintenanceState : IMaintenanceState
{
    /// <summary>0 = idle, 1 = a backup/restore holds the lock.</summary>
    private int _active;
    private MaintenanceStatus? _current;

    public bool IsActive => Volatile.Read(ref _active) == 1;

    public MaintenanceStatus? Current => Volatile.Read(ref _active) == 1 ? _current : null;

    public IDisposable Enter(string operation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);

        if (Interlocked.CompareExchange(ref _active, 1, 0) != 0)
        {
            throw new MaintenanceInProgressException(_current);
        }

        _current = new MaintenanceStatus(operation, DateTimeOffset.UtcNow);
        return new Lease(this);
    }

    private void Exit()
    {
        _current = null;
        Interlocked.Exchange(ref _active, 0);
    }

    private sealed class Lease(MaintenanceState owner) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                owner.Exit();
            }
        }
    }
}
