using Domain.Enums;

using System;

namespace Domain.Entities.Models;

public class PlayerSanction : EntityBase
{
    /// <summary>
    /// The sanction's length expressed in FECHAS (jornadas / matchdays), not
    /// calendar days (HU-75 / R1). A value of N means the subject is
    /// unavailable for the next N rounds of their team in the tournament. The
    /// day-based <c>GetExpiredSanctionsAsync</c> sweep is only a technical
    /// cleanup backstop and is NOT the source of truth for "fechas remaining".
    /// </summary>
    public required int Duration { get; set; }
    public required DateTime IssuedDate { get; set; }
    public required string Description { get; set; }

    /// <summary>
    /// What kind of subject this sanction targets (HU-77): a player, a team,
    /// or a staff member. Defaults to <see cref="SanctionSubjectType.Player"/>
    /// so every legacy sanction stays valid.
    /// </summary>
    public SanctionSubjectType SubjectType { get; set; } = SanctionSubjectType.Player;

    /// <summary>
    /// The sanctioned player. Only set (and required) when
    /// <see cref="SubjectType"/> is <see cref="SanctionSubjectType.Player"/>.
    /// </summary>
    public Player? Player { get; set; }
    public Guid? PlayerId { get; set; }

    /// <summary>
    /// The sanctioned team. Only set when <see cref="SubjectType"/> is
    /// <see cref="SanctionSubjectType.Team"/>.
    /// </summary>
    public Team? Team { get; set; }
    public Guid? TeamId { get; set; }

    /// <summary>
    /// The sanctioned staff member's name. Only set when
    /// <see cref="SubjectType"/> is <see cref="SanctionSubjectType.Staff"/>.
    /// Staff are not a modelled entity yet, so the subject is captured as a
    /// name rather than a foreign key.
    /// </summary>
    public string? StaffName { get; set; }

    public required Match Match { get; set; }
    public Guid MatchId { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public/admin sanction
    /// links. Generated once from the sanctioned subject's name and the
    /// sanction's issued date at creation time and never changed afterward,
    /// so shared links keep working even if the subject is renamed.
    /// </summary>
    public required string Slug { get; set; }

    public SanctionAppealStatus AppealStatus { get; set; } = SanctionAppealStatus.None;
    public string? AppealReason { get; set; }
    public DateTime? AppealDate { get; set; }
    public string? AppealResolution { get; set; }
    public DateTime? AppealResolvedDate { get; set; }
}
