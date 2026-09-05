namespace Domain.Enums;

/// <summary>
/// The kind of subject a PlayerSanction is applied to: an individual player, a whole team, or a staff member.
/// </summary>
public enum SanctionSubjectType
{
    /// <summary>
    /// The sanction targets an individual player, identified by PlayerSanction.PlayerId.
    /// </summary>
    Player,

    /// <summary>
    /// The sanction targets a whole team as an institutional sanction, identified by PlayerSanction.TeamId.
    /// </summary>
    Team,

    /// <summary>
    /// The sanction targets a staff member, identified only by PlayerSanction.StaffName.
    /// </summary>
    Staff
}
