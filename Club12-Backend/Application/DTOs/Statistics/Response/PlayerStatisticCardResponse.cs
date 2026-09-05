using System;
using System.Collections.Generic;

namespace Application.DTOs.Statistics.Response;

/// <summary>
/// A player's individual statistic card: total and average points and games played, per season and overall.
/// </summary>
public class PlayerStatisticCardResponse
{
    /// <summary>
    /// The player's stable identity, the same person across every season.
    /// </summary>
    public required Guid PlayerId { get; set; }

    /// <summary>
    /// The player's full name, formatted as LAST First Second.
    /// </summary>
    public required string FullName { get; set; }

    /// <summary>
    /// Overall total points across every season.
    /// </summary>
    public required int TotalPoints { get; set; }

    /// <summary>
    /// Overall distinct games played across every season.
    /// </summary>
    public required int GamesPlayed { get; set; }

    /// <summary>
    /// Overall points per game played, rounded to two decimals; zero when no games were played.
    /// </summary>
    public required double AveragePoints { get; set; }

    /// <summary>
    /// Per-season breakdown, most recent season first.
    /// </summary>
    public IEnumerable<SeasonStatLineResponse> Seasons { get; set; } = [];
}
