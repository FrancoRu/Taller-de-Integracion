using Application.DTOs.Abstract.Response;

using System;
using System.Collections.Generic;

namespace Application.DTOs.MatchSeries.Response;

/// <summary>
/// A best-of-N playoff series between two teams at one bracket round, including its individual games.
/// </summary>
public class MatchSeriesResponse : BaseEntityResponse
{
    public required Guid StageId { get; set; }

    public required Guid HomeTeamId { get; set; }

    public required string HomeTeamName { get; set; }

    public required Guid VisitorTeamId { get; set; }

    public required string VisitorTeamName { get; set; }

    /// <summary>
    /// Number of games in this series, one of 1, 3, 5, or 7.
    /// </summary>
    public required int BestOf { get; set; }

    /// <summary>
    /// The id of the winning team, set once one team has won the majority of the series' games.
    /// </summary>
    public Guid? WinningTeamId { get; set; }

    /// <summary>
    /// The name of the winning team, if the series has been decided.
    /// </summary>
    public string? WinningTeamName { get; set; }

    /// <summary>
    /// The individual games played so far in this series.
    /// </summary>
    public List<SeriesGameResponse> Games { get; set; } = [];
}
