using System;

namespace Application.DTOs.Champions.Response;

/// <summary>
/// One row of the champions history for a finished division, with context to render a table across seasons.
/// </summary>
public class ChampionHistoryResponse
{
    /// <summary>
    /// The id of the tournament the champion was crowned in.
    /// </summary>
    public required Guid TournamentId { get; set; }

    /// <summary>
    /// The name of the tournament.
    /// </summary>
    public required string TournamentName { get; set; }

    /// <summary>
    /// The season name, or null when the tournament is not grouped under any season.
    /// </summary>
    public string? SeasonName { get; set; }

    /// <summary>
    /// The season's calendar year, or null when there is no season or year set.
    /// </summary>
    public int? SeasonYear { get; set; }

    /// <summary>
    /// The competitive category of the division, Masculine or Feminine.
    /// </summary>
    public required string Category { get; set; }

    /// <summary>
    /// The name of the division that was won.
    /// </summary>
    public required string DivisionName { get; set; }

    /// <summary>
    /// The name of the sub-cup that was won, or null when the division crowns a single champion.
    /// </summary>
    public string? CupName { get; set; }

    /// <summary>
    /// The team that won this cup, in first place.
    /// </summary>
    public required PodiumTeamResponse ChampionTeam { get; set; }
}
