using Application.Interfaces.Backup;

using System;

namespace Application.Backup;

/// <summary>
/// In-memory IMaintenanceModeState singleton — pure process state
/// with no OS/network I/O, same category as
/// KeepLastNRetentionPolicy (design.md: "Maintenance state lives in
/// Application/Backup, not Infrastructure"). Not thread-synchronized beyond
/// field assignment ordering: Enter/Exit are only ever invoked while the
/// caller already holds BackupOperationLock, so there is no
/// concurrent-writer race in practice; reads (e.g. by the middleware) are
/// always safe since every field is written together.
/// </summary>
public sealed class MaintenanceModeState : IMaintenanceModeState
{
    public bool IsActive { get; private set; }

    public string? Reason { get; private set; }

    public DateTimeOffset? EnteredAtUtc { get; private set; }

    public void Enter(string reason)
    {
        Reason = reason;
        EnteredAtUtc = DateTimeOffset.UtcNow;
        IsActive = true;
    }

    public void Exit()
    {
        IsActive = false;
        Reason = null;
        EnteredAtUtc = null;
    }
}
