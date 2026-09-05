using Application.DTOs.Abstract.Response;
using Application.DTOs.Team.Response;
using Application.DTOs.Venue.Response;

using System;

namespace Application.DTOs.Match.Response;

/// <summary>
/// Represents the response data for a match.
/// </summary>
public class DetailedMatchResponse : BaseEntityResponse
{
    public required DateTime MatchDate { get; set; }

    /// <summary>
    /// The 1-based matchday this match belongs to; null for stages with no round-robin matchday.
    /// </summary>
    public int? Round { get; set; }

    /// <summary>
    /// The type of the match, regular or playoff.
    /// </summary>
    public required string MatchType { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public match links.
    /// </summary>
    public required string Slug { get; set; }

    public TeamDetailedMatchResponse? HomeTeam { get; set; }

    public TeamDetailedMatchResponse? VisitorTeam { get; set; }

    public VenueResponse? Venue { get; set; }

    public required bool IsFinished { get; set; }

    /// <summary>
    /// The result lifecycle state: Scheduled, Played, Suspended, or WalkOver.
    /// </summary>
    public string? Status { get; set; }

    public bool WentToOvertime { get; set; }

    public string? WinningTeamName { get; set; }

    public Guid? WinningTeamId { get; set; }

    public Guid? StageId { get; set; }

    /// <summary>
    /// The id of the tournament this match belongs to, letting the public page link back to it directly.
    /// </summary>
    public Guid? TournamentId { get; set; }
}
