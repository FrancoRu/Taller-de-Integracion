using Application.Interfaces.Backup;

using System;

namespace Application.Backup;

/// <summary>
/// In-memory IMaintenanceModeState singleton, pure process state with no OS or network I/O.
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
