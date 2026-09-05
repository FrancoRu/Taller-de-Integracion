using Application.DTOs.Abstract.Response;

using System;

namespace Application.DTOs.Match.Response;

/// <summary>
/// Represents the minimal response data for a match, tailored for divisions.
/// </summary>
public class MinimalMatchResponse : BaseEntityResponse
{
    public required DateTime MatchDate { get; set; }

    /// <summary>
    /// The matchday (jornada) this match belongs to, 1-based (HU-63/HU-65).
    /// Canonical fixture grouping key; null for matches with no round-robin
    /// matchday (e.g. knockout stages).
    /// </summary>
    public int? Round { get; set; }

    public required string HomeTeamName { get; set; }

    public required string VisitorTeamName { get; set; }

    public int? HomeScore { get; set; }

    public int? VisitorScore { get; set; }

    public string? WinningTeamName { get; set; }

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
