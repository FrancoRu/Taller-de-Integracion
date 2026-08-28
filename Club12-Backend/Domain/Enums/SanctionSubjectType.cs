namespace Domain.Enums;

/// <summary>
/// The kind of subject a <see cref="Domain.Entities.Models.PlayerSanction"/>
/// is applied to (HU-77). A sanction can target an individual player, a whole
/// team, or a staff member.
/// </summary>
public enum SanctionSubjectType
{
    /// <summary>
    /// The sanction targets an individual player. This is the default and the
    /// only value legacy sanctions ever had, so existing rows remain valid.
    /// The player is identified by <see cref="Domain.Entities.Models.PlayerSanction.PlayerId"/>.
    /// </summary>
    Player,

    /// <summary>
    /// The sanction targets a whole team (an institutional sanction). The team
    /// is identified by <see cref="Domain.Entities.Models.PlayerSanction.TeamId"/>.
    /// </summary>
    Team,

    /// <summary>
    /// The sanction targets a staff member. Staff are not modelled as a
    /// first-class entity in this codebase yet, so the subject is identified
    /// only by <see cref="Domain.Entities.Models.PlayerSanction.StaffName"/>.
    /// </summary>
    Staff
}
