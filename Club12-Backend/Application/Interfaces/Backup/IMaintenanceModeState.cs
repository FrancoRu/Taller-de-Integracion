using System;

namespace Application.Interfaces.Backup;

/// <summary>
/// Process-wide maintenance-mode flag. While active,
/// MaintenanceModeMiddleware (a later work unit) returns 503 for
/// every request except the /health* and /api/maintenance
/// allow-listed paths (design.md's "Maintenance gate" decision).
/// Registered as a singleton — in-memory only, so a process restart is
/// itself an escape hatch for a stuck window (see design.md's "Stuck
/// maintenance window" rollout note).
/// </summary>
public interface IMaintenanceModeState
{
    bool IsActive { get; }

    string? Reason { get; }

    DateTimeOffset? EnteredAtUtc { get; }

    /// <summary>
    /// Enters maintenance mode with <paramref name="reason"/>. Calling this
    /// again while already active overwrites the previous reason/timestamp
    /// with the new one.
    /// </summary>
    void Enter(string reason);

    /// <summary>
    /// Exits maintenance mode. Safe to call even when not currently active —
    /// this is what the manual escape hatch (DELETE api/maintenance)
    /// relies on to clear a stuck window with no in-flight restore.
    /// </summary>
    void Exit();
}
