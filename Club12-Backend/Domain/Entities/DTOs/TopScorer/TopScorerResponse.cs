namespace Entities.DTOs.TopScorer;


/// <summary>
/// Represents the response model for top scorer data, including weekly scores.
/// </summary>
public class TopScorerResponse
{
    /// <summary>
    /// The unique identifier of the player.
    /// </summary>
    public required Guid PlayerId { get; set; }

    /// <summary>
    /// The first name of the player.
    /// </summary>
    public required string FirstName { get; set; }

    /// <summary>
    /// The last name of the player.
    /// </summary>
    public required string LastName { get; set; }

    /// <summary>
    /// A dictionary of weekly scores for the player.
    /// The key is the week number, and the value is the player's score in that week.
    /// </summary>
    public required Dictionary<int, int> WeeklyScores { get; set; }

    /// <summary>
    /// The total points scored by the player across all weeks.
    /// </summary>
    public int TotalPoints { get; set; }
}