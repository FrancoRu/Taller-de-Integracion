namespace Domain.Enums;

/// <summary>
/// Medical-record eligibility status of a player's season registration, scoped per player, team, and tournament.
/// </summary>
public enum MedicalRecordStatus
{
    /// <summary>
    /// Default for a brand-new registration: no medical record uploaded yet, or uploaded but not yet reviewed.
    /// </summary>
    Pending,

    /// <summary>
    /// The owner or admin reviewed and approved the medical record, making the player habilitado for that team and tournament.
    /// </summary>
    Approved,

    /// <summary>
    /// The owner or admin rejected the medical record, usually with a reason, leaving the player not habilitado.
    /// </summary>
    Rejected,
}
