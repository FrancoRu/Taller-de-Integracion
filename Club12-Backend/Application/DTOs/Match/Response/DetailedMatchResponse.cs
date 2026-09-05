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

    public TeamDetailedMatchResponse? HomeTeam { get; set; }

    public TeamDetailedMatchResponse? VisitorTeam { get; set; }

    public VenueResponse? Venue { get; set; }

    public required bool IsFinished { get; set; }

    /// <summary>
    /// The result lifecycle state (HU-69): Scheduled, Played, Suspended, or
    /// WalkOver. Lets the UI distinguish a walkover from a normal result.
    /// </summary>
    public string? Status { get; set; }

    public bool WentToOvertime { get; set; }

    public string? WinningTeamName { get; set; }

    public Guid? WinningTeamId { get; set; }

    public Guid? StageId { get; set; }

    /// <summary>
    /// The Id of the tournament this match belongs to (via Stage.Division),
    /// so the public match page can navigate back to its tournament instead
    /// of a generic listing.
    /// </summary>
    public Guid? TournamentId { get; set; }
}
