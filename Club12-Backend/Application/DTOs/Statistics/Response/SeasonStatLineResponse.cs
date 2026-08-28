namespace Application.DTOs.Statistics.Response;

/// <summary>
/// One season's scoring line in a player's statistic card (HU-87). A "season"
/// is the calendar year of the tournament's start date (HU-85).
/// </summary>
public class SeasonStatLineResponse
{
    /// <summary>The season (calendar year of the tournament's start date).</summary>
    public required int Season { get; set; }

    /// <summary>Total points the player scored in that season.</summary>
    public required int TotalPoints { get; set; }

    /// <summary>Distinct matches the player appeared in (had a Points sheet entry for) that season.</summary>
    public required int GamesPlayed { get; set; }

    /// <summary>
    /// Points per game played in that season (TotalPoints / GamesPlayed),
    /// rounded to two decimals. Zero when the player played no games.
    /// </summary>
    public required double AveragePoints { get; set; }
}
