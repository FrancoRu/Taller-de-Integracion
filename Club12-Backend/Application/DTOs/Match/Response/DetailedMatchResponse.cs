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
    /// <summary>
    /// The date of the match.
    /// </summary>
    public required DateTime MatchDate { get; set; }

    /// <summary>
    /// The matchday (jornada) this match belongs to, 1-based (HU-63/HU-65).
    /// This is the canonical grouping key for the fixture ("Fecha 1", "Fecha
    /// 2", …); the frontend should group by this rather than by MatchDate. Null
    /// for matches with no round-robin matchday (e.g. knockout stages).
    /// </summary>
    public int? Round { get; set; }

    /// <summary>
    /// The type of the match (e.g., regular or playoff).
    /// </summary>
    public required string MatchType { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public match links.
    /// </summary>
    public required string Slug { get; set; }

    /// <summary>
    /// The team details for the home team.
    /// </summary>
    public TeamDetailedMatchResponse? HomeTeam { get; set; }

    /// <summary>
    /// The team details for the visitor team.
    /// </summary>
    public TeamDetailedMatchResponse? VisitorTeam { get; set; }

    /// <summary>
    /// The venue where the match is played.
    /// </summary>
    public VenueResponse? Venue { get; set; }

    /// <summary>
    /// Whether the match has finished.
    /// </summary>
    public required bool IsFinished { get; set; }

    /// <summary>
    /// The result lifecycle state (HU-69): Scheduled, Played, Suspended, or
    /// WalkOver. Lets the UI distinguish a walkover from a normal result.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// The name of the winning team, if available.
    /// </summary>
    public string? WinningTeamName { get; set; }

    /// <summary>
    /// The Id of the winning team, if available.
    /// </summary>
    public Guid? WinningTeamId { get; set; }

    /// <summary>
    /// The Id of the stage.
    /// </summary>
    public Guid? StageId { get; set; }

    /// <summary>
    /// The Id of the tournament this match belongs to (via Stage.Division),
    /// so the public match page can navigate back to its tournament instead
    /// of a generic listing.
    /// </summary>
    public Guid? TournamentId { get; set; }
}
