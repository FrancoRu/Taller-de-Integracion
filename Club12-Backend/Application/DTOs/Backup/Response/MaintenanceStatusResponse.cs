using System;

namespace Application.DTOs.Backup.Response;

/// <summary>
/// API projection of IMaintenanceModeState — the current maintenance
/// window (if any). Reason/EnteredAtUtc are null when
/// IsActive is false.
/// </summary>
public sealed record MaintenanceStatusResponse(bool IsActive, string? Reason, DateTimeOffset? EnteredAtUtc);
