using Entities.DTOs.Abstract;

namespace Entities.DTOs.Match;

/// <summary>
/// Represents the response data for a match.
/// </summary>
public class MatchResponse : BaseEntityResponse
{
    /// <summary>
    /// The date of the match.
    /// </summary>
    public required DateTime MatchDate { get; set; }

    /// <summary>
    /// The type of the match (e.g., regular or playoff).
    /// </summary>
    public required MatchType Type { get; set; }

    /// <summary>
    /// The unique identifier of the home team.
    /// </summary>
    public required Guid HomeTeamId { get; set; }

    /// <summary>
    /// The name of the home team.
    /// </summary>
    public required string HomeTeamName { get; set; }

    /// <summary>
    /// The unique identifier of the visitor team.
    /// </summary>
    public required Guid VisitorTeamId { get; set; }

    /// <summary>
    /// The name of the visitor team.
    /// </summary>
    public required string VisitorTeamName { get; set; }

    /// <summary>
    /// The score of the home team.
    /// </summary>
    public int? HomeScore { get; set; }

    /// <summary>
    /// The score of the visitor team.
    /// </summary>
    public int? VisitorScore { get; set; }

    /// <summary>
    /// Whether the match has finished.
    /// </summary>
    public required bool IsFinished { get; set; }

    /// <summary>
    /// The name of the winning team, if available.
    /// </summary>
    public string? WinningTeamName { get; set; }
}
