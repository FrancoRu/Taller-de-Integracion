using System;
using System.Collections.Generic;

namespace Application.DTOs.Statistics.Response;

/// <summary>
/// One row of a player's trajectory, sourced from the season-scoped registration to preserve past history.
/// </summary>
public class PlayerHistorySeasonResponse
{
    /// <summary>
    /// The season, as the calendar year of the tournament's start date.
    /// </summary>
    public required int Season { get; set; }

    public required Guid TournamentId { get; set; }

    public required string TournamentName { get; set; }

    /// <summary>
    /// The team the player was registered to for that season.
    /// </summary>
    public required Guid TeamId { get; set; }

    public required string TeamName { get; set; }

    /// <summary>
    /// Total points scored for that season's tournament.
    /// </summary>
    public required int TotalPoints { get; set; }

    /// <summary>
    /// Distinct games played in that season's tournament.
    /// </summary>
    public required int GamesPlayed { get; set; }

    /// <summary>
    /// Sanctions received during that season's tournament.
    /// </summary>
    public IEnumerable<PlayerHistorySanctionResponse> Sanctions { get; set; } = [];
}
