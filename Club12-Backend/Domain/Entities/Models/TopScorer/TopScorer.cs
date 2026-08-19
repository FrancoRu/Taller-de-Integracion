namespace Entities.Models.TopScorerModel;


/// <summary>
/// Represents a top scorer player with their scores across multiple weeks.
/// </summary>
public class TopScorer
{
    /// <summary>
    /// Gets or sets the unique identifier of the player.
    /// </summary>
    public Guid PlayerId { get; set; }

    /// <summary>
    /// Gets or sets the first name of the player.
    /// </summary>
    public required string FirstName { get; set; }

    /// <summary>
    /// Gets or sets the last name of the player.
    /// </summary>
    public required string LastName { get; set; }

    /// <summary>
    /// Gets or sets a dictionary of weekly scores for the player.
    /// The key is the week number, and the value is the player's score in that week.
    /// </summary>
    public required Dictionary<int, int> WeeklyScores { get; set; }

    /// <summary>
    /// The total points the player has scored across all weeks.
    /// </summary>
    public int TotalPoints => WeeklyScores.Values.Sum();
}