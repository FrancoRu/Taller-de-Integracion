using Application.DTOs.Backup.Response;

namespace Application.Interfaces.Backup;

/// <summary>
/// Outcome of a single IBackupOperationsService call. Controllers
/// map this explicitly to an HTTP status code instead of relying on
/// exception-mapped status codes, keeping controller tests pure (see
/// design.md's "Controllers return an explicit outcome" decision).
/// </summary>
public enum BackupOperationOutcome
{
    Completed,
    Busy,
    NotFound,
    Failed,
}

/// <summary>
/// Result of a create/delete/restore attempt. Record is populated
/// only when Outcome is Completed; Message carries a
/// human-readable detail for Busy/Failed outcomes.
/// </summary>
public sealed record BackupOperationResult(
    BackupOperationOutcome Outcome, BackupRecordResponse? Record, string? Message);
