using System;
using System.Collections.Generic;

namespace Domain.Entities.Models;

public class Team : EntityBase
{
    public required string Name { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public team links, generated once from the name and never changed afterward.
    /// </summary>
    public required string Slug { get; set; }

    public required string ThreeLetterCode { get; set; }
    public required string LogoUrl { get; set; }
    public required string ShirtColor { get; set; }

    /// <summary>
    /// The jersey kit pattern applied over the primary ShirtColor, defaulting to solid.
    /// </summary>
    public string JerseyStyle { get; set; } = "solid";

    /// <summary>
    /// Optional secondary hex color used for the jersey pattern or trim, null when the kit is a plain solid with no accent.
    /// </summary>
    public string? ShirtSecondaryColor { get; set; }

    /// <summary>
    /// Optional third hex color, used only by tri-color kit templates as a second accent alongside ShirtSecondaryColor.
    /// </summary>
    public string? ShirtTertiaryColor { get; set; }

    public Tournament? Tournament { get; set; }
    public Guid? TournamentId { get; set; }

    /// <summary>
    /// Optional link to the team's stable cross-season identity, purely additive and never breaking existing per-season Team behavior.
    /// </summary>
    public Guid? ClubId { get; set; }
    public Club? Club { get; set; }
    /// <summary>
    /// Players whose denormalized current team pointer points here, not season-scoped.
    /// </summary>
    public virtual required ICollection<Player> Players { get; set; } = [];
    public virtual ICollection<StageTeamMatch> StageTeamMatches { get; set; } = [];

    /// <summary>
    /// Every player ever registered to this team, across every season it has belonged to, the source of truth for roster membership.
    /// </summary>
    public virtual ICollection<PlayerTeamRegistration> PlayerTeamRegistrations { get; set; } = [];

    /// <summary>
    /// Every tournament this team has ever been registered to, across every season, independent of the current TournamentId pointer.
    /// </summary>
    public virtual ICollection<TeamTournamentRegistration> TeamTournamentRegistrations { get; set; } = [];
}