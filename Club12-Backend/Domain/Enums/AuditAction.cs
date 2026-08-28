namespace Domain.Enums;

/// <summary>
/// The sensitive, auditable actions tracked for traceability (HU-101).
/// Persisted as the enum name (string) so the audit trail stays readable and
/// stable even if the numeric order changes.
/// </summary>
public enum AuditAction
{
    /// <summary>Full tournament-domain data wipe (DataMaintenanceService).</summary>
    DataWipe,

    /// <summary>Database restore from a backup (BackupRestoreService).</summary>
    BackupRestore,

    /// <summary>A tournament lifecycle status change (TournamentService).</summary>
    TournamentStatusChange,

    /// <summary>An admin-triggered password reset / blanqueo (user management).</summary>
    PasswordReset,
}
