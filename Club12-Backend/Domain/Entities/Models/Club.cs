using System.Collections.Generic;

namespace Domain.Entities.Models;

/// <summary>
/// A club is the STABLE identity that persists across seasons (HU-99). Unlike
/// a <see cref="Team"/> — which is a per-season registration record and may be
/// re-created or re-pointed every tournament — a Club is written once and never
/// changes meaning, so "Colón SF" is recognizable as the same club across every
/// season even though each season is a distinct <see cref="Team"/> row. Teams
/// hang off a Club through the optional <see cref="Team.ClubId"/> FK; a Team
/// with a null ClubId is simply not yet linked and keeps working exactly as
/// before (this relationship is purely additive).
/// </summary>
public class Club : EntityBase
{
    /// <summary>The club's stable display name, e.g. "Colón SF".</summary>
    public required string Name { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public club links. Generated
    /// once from the name at creation time and never changed afterward.
    /// </summary>
    public required string Slug { get; set; }

    /// <summary>Optional club crest/logo URL. Independent of any season team logo.</summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Every per-season <see cref="Team"/> that belongs to this club, across
    /// every tournament. The join back to season participation is each team's
    /// <see cref="Team.TeamTournamentRegistrations"/>.
    /// </summary>
    public virtual ICollection<Team> Teams { get; set; } = [];
}
