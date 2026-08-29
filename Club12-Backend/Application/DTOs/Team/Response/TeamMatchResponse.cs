using System;

namespace Application.DTOs.Team.Response;

/// <summary>
/// A single match of a team, projected from that team's point of view for the
/// public team-profile fixture/results list. Scores and the win/loss result are
/// oriented relative to the team, not to home/visitor — see
/// <see cref="Application.Utils.Helper.TeamProfile.TeamMatchMapper"/>.
/// </summary>
public class TeamMatchResponse
{
    /// <summary>The id of the match.</summary>
    public required Guid MatchId { get; set; }

    /// <summary>The scheduled/played calendar date of the match.</summary>
    public DateTime? MatchDate { get; set; }

    /// <summary>Whether the match has finished (a decisive result exists).</summary>
    public required bool IsFinished { get; set; }

    /// <summary>The match lifecycle status name (Scheduled/Played/Suspended/WalkOver).</summary>
    public required string Status { get; set; }

    /// <summary>True when the team played this match at home.</summary>
    public required bool IsHome { get; set; }

    /// <summary>The opponent team's id; <see cref="Guid.Empty"/> when the opponent slot is unassigned.</summary>
    public required Guid OpponentTeamId { get; set; }

    /// <summary>The opponent team's name; empty when the opponent slot is unassigned.</summary>
    public required string OpponentName { get; set; }

    /// <summary>The opponent team's logo URL, when available.</summary>
    public string? OpponentLogoUrl { get; set; }

    /// <summary>The team's own score, when the match has a result.</summary>
    public int? TeamScore { get; set; }

    /// <summary>The opponent's score, when the match has a result.</summary>
    public int? OpponentScore { get; set; }

    /// <summary>"W" or "L" from the team's perspective when finished; null otherwise.</summary>
    public string? Result { get; set; }

    /// <summary>The venue's name, when the match has a venue assigned.</summary>
    public string? VenueName { get; set; }
}
