namespace Domain.Enums;

/// <summary>
/// How a catalogued database backup was created.
/// </summary>
public enum BackupOrigin
{
    /// <summary>
    /// Triggered on-demand by an Admin from the Data Administration panel.
    /// </summary>
    Manual,

    /// <summary>
    /// Created automatically by the scheduled DatabaseBackupHostedService.
    /// </summary>
    Job
}
