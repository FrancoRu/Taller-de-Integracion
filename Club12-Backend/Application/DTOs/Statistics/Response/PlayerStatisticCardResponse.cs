using System;
using System.Collections.Generic;

namespace Application.DTOs.Statistics.Response;

/// <summary>
/// A player's individual statistic card (HU-87): total and average points and
/// games played, both per season and overall. Every season is a distinct
/// registration (HU-98), but all seasons are aggregated under one person via
/// the stable PlayerId (guaranteed one-per-person by Player.DocumentNumber's
/// unique index).
/// </summary>
public class PlayerStatisticCardResponse
{
    /// <summary>The player's stable identity (same person across every season).</summary>
    public required Guid PlayerId { get; set; }

    /// <summary>The player's full name (LAST First Second).</summary>
    public required string FullName { get; set; }

    /// <summary>Overall total points across every season.</summary>
    public required int TotalPoints { get; set; }

    /// <summary>Overall distinct games played across every season.</summary>
    public required int GamesPlayed { get; set; }

    /// <summary>Overall points per game played, rounded to two decimals. Zero when no games.</summary>
    public required double AveragePoints { get; set; }

    /// <summary>Per-season breakdown, most recent season first.</summary>
    public IEnumerable<SeasonStatLineResponse> Seasons { get; set; } = [];
}
