using System;

namespace Application.Interfaces.Backup;

/// <summary>
/// Process-wide maintenance-mode flag registered as a singleton, in-memory only.
/// </summary>
public interface IMaintenanceModeState
{
    bool IsActive { get; }

    string? Reason { get; }

    DateTimeOffset? EnteredAtUtc { get; }

    /// <summary>
    /// Enters maintenance mode with reason.
    /// </summary>
    void Enter(string reason);

    /// <summary>
    /// Exits maintenance mode and is safe to call even when not currently active.
    /// </summary>
    void Exit();
}
