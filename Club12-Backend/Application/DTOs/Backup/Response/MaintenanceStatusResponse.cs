using System;

namespace Application.DTOs.Backup.Response;

/// <summary>
/// The current maintenance window; Reason and EnteredAtUtc are null when IsActive is false.
/// </summary>
public sealed record MaintenanceStatusResponse(bool IsActive, string? Reason, DateTimeOffset? EnteredAtUtc);
