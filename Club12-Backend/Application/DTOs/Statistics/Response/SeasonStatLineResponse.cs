namespace Application.DTOs.Statistics.Response;

/// <summary>
/// One season's scoring line in a player's statistic card, where a season is a tournament start-date year.
/// </summary>
public class SeasonStatLineResponse
{
    /// <summary>
    /// The season, as the calendar year of the tournament's start date.
    /// </summary>
    public required int Season { get; set; }

    /// <summary>
    /// Total points the player scored in that season.
    /// </summary>
    public required int TotalPoints { get; set; }

    /// <summary>
    /// Distinct matches the player had a scoring-sheet entry for in that season.
    /// </summary>
    public required int GamesPlayed { get; set; }

    /// <summary>
    /// Points per game played in that season, rounded to two decimals; zero when no games were played.
    /// </summary>
    public required double AveragePoints { get; set; }
}
