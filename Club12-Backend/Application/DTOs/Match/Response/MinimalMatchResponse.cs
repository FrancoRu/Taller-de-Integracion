using Application.DTOs.Abstract.Response;

using System;

namespace Application.DTOs.Match.Response;

/// <summary>
/// Represents the minimal response data for a match, tailored for divisions.
/// </summary>
public class MinimalMatchResponse : BaseEntityResponse
{
    /// <summary>
    /// The date of the match.
    /// </summary>
    public required DateTime MatchDate { get; set; }

    /// <summary>
    /// The name of the home team.
    /// </summary>
    public required string HomeTeamName { get; set; }

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
    /// The name of the winning team, if available.
    /// </summary>
    public string? WinningTeamName { get; set; }

    /// <summary>
    /// Whether the match has finished.
    /// </summary>
    public required bool IsFinished { get; set; }

    /// <summary>
    /// The result lifecycle state (HU-69): Scheduled, Played, Suspended, or
    /// WalkOver.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// The type of the match (e.g., regular or playoff).
    /// </summary>
    public required string MatchType { get; set; }
}
