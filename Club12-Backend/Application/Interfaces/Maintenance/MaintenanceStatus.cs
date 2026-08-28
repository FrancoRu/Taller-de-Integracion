using System;

namespace Application.Interfaces.Maintenance;

/// <summary>
/// Snapshot of the current maintenance operation (HU-92): what is running
/// (<paramref name="Operation"/>, e.g. "backup" or "restore") and when it
/// started. Present only while the app is locked.
/// </summary>
public sealed record MaintenanceStatus(string Operation, DateTimeOffset StartedAt);
