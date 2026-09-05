using Application.DTOs.Backup.Response;

namespace Application.Interfaces.Backup;

/// <summary>
/// Result of a create, delete, or restore attempt, with Record populated only when Outcome is Completed.
/// </summary>
public sealed record BackupOperationResult(
    BackupOperationOutcome Outcome, BackupRecordResponse? Record, string? Message);
