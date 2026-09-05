using System;

namespace Application.DTOs.Team.Response;

/// <summary>
/// A team's current standing row inside its group-stage table for a single tournament.
/// </summary>
public class TeamSummaryResponse
{
    /// <summary>
    /// The id of the division whose group standings contain the team.
    /// </summary>
    public required Guid DivisionId { get; set; }

    /// <summary>
    /// The division's display name.
    /// </summary>
    public required string DivisionName { get; set; }

    /// <summary>
    /// The team's 1-based rank within its group table.
    /// </summary>
    public required int Position { get; set; }

    /// <summary>
    /// The number of teams in the same group table.
    /// </summary>
    public required int TotalTeams { get; set; }

    /// <summary>
    /// Matches played by the team in the group stage.
    /// </summary>
    public required int Played { get; set; }

    /// <summary>
    /// Matches won.
    /// </summary>
    public required int Wins { get; set; }

    /// <summary>
    /// Matches lost.
    /// </summary>
    public required int Losses { get; set; }

    /// <summary>
    /// Total points scored by the team.
    /// </summary>
    public required int PointsFor { get; set; }

    /// <summary>
    /// Total points scored against the team.
    /// </summary>
    public required int PointsAgainst { get; set; }

    /// <summary>
    /// PointsFor minus PointsAgainst.
    /// </summary>
    public required int PointsDifference { get; set; }

    /// <summary>
    /// Table points accumulated, per the division's configured win and loss values.
    /// </summary>
    public required int Points { get; set; }
}
