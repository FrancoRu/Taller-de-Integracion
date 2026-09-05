using System;

namespace Application.DTOs.Team.Response;

/// <summary>
/// One tournament a team participated in, enriched with season info for the public trajectory list.
/// </summary>
public class TeamParticipationResponse
{
    /// <summary>
    /// The id of the tournament the team participated in.
    /// </summary>
    public required Guid TournamentId { get; set; }

    /// <summary>
    /// The tournament's display name.
    /// </summary>
    public required string TournamentName { get; set; }

    /// <summary>
    /// The tournament's public slug.
    /// </summary>
    public string? TournamentSlug { get; set; }

    /// <summary>
    /// The tournament's competitive category name, Masculine or Feminine.
    /// </summary>
    public required string Category { get; set; }

    /// <summary>
    /// The id of the season the tournament belongs to, when grouped under one.
    /// </summary>
    public Guid? SeasonId { get; set; }

    /// <summary>
    /// The season's display name, when the tournament belongs to a season.
    /// </summary>
    public string? SeasonName { get; set; }

    /// <summary>
    /// The season's calendar year, when known.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// True when this is the team's current tournament.
    /// </summary>
    public required bool IsCurrent { get; set; }
}
