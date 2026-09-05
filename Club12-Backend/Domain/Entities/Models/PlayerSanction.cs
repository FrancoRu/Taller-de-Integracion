using Domain.Enums;

using System;

namespace Domain.Entities.Models;

public class PlayerSanction : EntityBase
{
    /// <summary>
    /// The sanction's length expressed in fechas, not calendar days: a value of N means the subject sits out the next N rounds.
    /// </summary>
    public required int Duration { get; set; }
    public required DateTime IssuedDate { get; set; }
    public required string Description { get; set; }

    /// <summary>
    /// What kind of subject this sanction targets: a player, a team, or a staff member. Defaults to Player.
    /// </summary>
    public SanctionSubjectType SubjectType { get; set; } = SanctionSubjectType.Player;

    /// <summary>
    /// The sanctioned player, only set and required when SubjectType is Player.
    /// </summary>
    public Player? Player { get; set; }
    public Guid? PlayerId { get; set; }

    /// <summary>
    /// The sanctioned team, only set when SubjectType is Team.
    /// </summary>
    public Team? Team { get; set; }
    public Guid? TeamId { get; set; }

    /// <summary>
    /// The sanctioned staff member's name, only set when SubjectType is Staff.
    /// </summary>
    public string? StaffName { get; set; }

    public required Match Match { get; set; }
    public Guid MatchId { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public and admin sanction links, generated once from the subject's name and the issued date.
    /// </summary>
    public required string Slug { get; set; }

    public SanctionAppealStatus AppealStatus { get; set; } = SanctionAppealStatus.None;
    public string? AppealReason { get; set; }
    public DateTime? AppealDate { get; set; }
    public string? AppealResolution { get; set; }
    public DateTime? AppealResolvedDate { get; set; }
}
