using System;

namespace Application.Interfaces.Maintenance;

/// <summary>
/// App-wide maintenance lock (HU-92). A single backup or restore holds this
/// lock for its whole duration; while it is held, the maintenance middleware
/// short-circuits mutating requests so no user can modify data mid-operation.
/// Registered as a singleton so the middleware and the backup/restore
/// services share one instance.
/// </summary>
public interface IMaintenanceState
{
    /// <summary>
    /// True while a backup/restore holds the lock.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// The running operation's status, or null when idle.
    /// </summary>
    MaintenanceStatus? Current { get; }

    /// <summary>
    /// Acquires the lock for <paramref name="operation"/> and returns a lease;
    /// dispose it (typically via <c>using</c>) to release. Throws
    /// <see cref="MaintenanceInProgressException"/> if the lock is already
    /// held — the caller should surface that as "operation in progress".
    /// </summary>
    IDisposable Enter(string operation);
}
