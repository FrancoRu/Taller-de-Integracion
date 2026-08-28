namespace Domain.Enums;

/// <summary>
/// Medical-record / eligibility status of a player's season registration
/// (HU-57). Scoped per PlayerTeamRegistration — i.e. per player + team +
/// tournament — so being Approved in one season never carries over to another
/// (HU-59). A player is "habilitado" for that team+tournament only when the
/// record is <see cref="Approved"/>.
/// </summary>
public enum MedicalRecordStatus
{
    /// <summary>
    /// Default for a brand-new registration: no medical record uploaded yet,
    /// or uploaded but not yet reviewed. The player is NOT habilitado.
    /// </summary>
    Pending,

    /// <summary>
    /// The owner/admin reviewed and approved the medical record (HU-58): the
    /// player is habilitado for that team and tournament (HU-57).
    /// </summary>
    Approved,

    /// <summary>
    /// The owner/admin rejected the medical record (HU-58), usually with a
    /// reason. The player is NOT habilitado.
    /// </summary>
    Rejected,
}
