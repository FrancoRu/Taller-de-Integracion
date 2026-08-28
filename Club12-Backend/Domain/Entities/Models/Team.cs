using System;
using System.Collections.Generic;

namespace Domain.Entities.Models;

public class Team : EntityBase
{
    public required string Name { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public team links.
    /// Generated once from the name at creation time and never changed afterward,
    /// so shared links keep working even if the team is renamed.
    /// </summary>
    public required string Slug { get; set; }

    public required string ThreeLetterCode { get; set; }
    public required string LogoUrl { get; set; }
    public required string ShirtColor { get; set; }
    public Tournament? Tournament { get; set; }
    public Guid? TournamentId { get; set; }

    /// <summary>
    /// Optional link to the team's stable cross-season identity (HU-99). Null
    /// for teams that have not been linked to a <see cref="Club"/> yet — such
    /// teams keep working exactly as before, so this FK is purely additive and
    /// never breaks existing per-season Team behavior. When set, all the
    /// season Team rows for the same real-world club (e.g. "Colón SF 2026" and
    /// "Colón SF 2027") share one <see cref="ClubId"/>, which is what makes a
    /// club's trajectory resolvable across seasons.
    /// </summary>
    public Guid? ClubId { get; set; }
    public Club? Club { get; set; }
    /// <summary>
    /// Players whose denormalized CURRENT team (<see cref="Player.TeamId"/>)
    /// points here. Not season-scoped — do not use this for roster display;
    /// use <see cref="PlayerTeamRegistrations"/> filtered by TournamentId instead.
    /// </summary>
    public virtual required ICollection<Player> Players { get; set; } = [];
    public virtual ICollection<StageTeamMatch> StageTeamMatches { get; set; } = [];

    /// <summary>
    /// Every player ever registered to this team, across every season it
    /// has belonged to. The source of truth for roster membership — see
    /// <see cref="PlayerTeamRegistration"/>.
    /// </summary>
    public virtual ICollection<PlayerTeamRegistration> PlayerTeamRegistrations { get; set; } = [];

    /// <summary>
    /// Every tournament this team has ever been registered to, across every
    /// season, independent of the current <see cref="TournamentId"/>
    /// pointer. The source of truth for season-scoped participation — see
    /// <see cref="TeamTournamentRegistration"/>.
    /// </summary>
    public virtual ICollection<TeamTournamentRegistration> TeamTournamentRegistrations { get; set; } = [];
}