namespace Domain.Enums;

/// <summary>
/// The sensitive, auditable actions tracked for traceability, persisted as the enum name so the trail survives numeric reordering.
/// </summary>
public enum AuditAction
{
    /// <summary>
    /// Full tournament-domain data wipe, triggered by DataMaintenanceService.
    /// </summary>
    DataWipe,

    /// <summary>
    /// Database restore from a backup, triggered by BackupRestoreService.
    /// </summary>
    BackupRestore,

    /// <summary>
    /// A tournament lifecycle status change, triggered by TournamentService.
    /// </summary>
    TournamentStatusChange,

    /// <summary>
    /// An admin-triggered password reset, from user management.
    /// </summary>
    PasswordReset,
}
