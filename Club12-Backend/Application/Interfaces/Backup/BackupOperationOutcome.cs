namespace Application.Interfaces.Backup;

/// <summary>
/// Outcome of a single IBackupOperationsService call, mapped explicitly by controllers to an HTTP status code.
/// </summary>
public enum BackupOperationOutcome
{
    Completed,
    Busy,
    NotFound,
    Failed,
}
