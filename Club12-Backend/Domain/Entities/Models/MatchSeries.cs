using System;
using System.Collections.Generic;

namespace Domain.Entities.Models;

/// <summary>
/// A best-of-N playoff series between two teams at one bracket round.
/// Groups the individual Match games that make up the series and tracks
/// the winner once one team has won the majority of BestOf games.
/// </summary>
public class MatchSeries : EntityBase
{
    public required Guid StageId { get; set; }
    public Stage? Stage { get; set; }

    public required Guid HomeTeamId { get; set; }
    public Team? HomeTeam { get; set; }

    public required Guid VisitorTeamId { get; set; }
    public Team? VisitorTeam { get; set; }

    /// <summary>
    /// Number of games in this series (1, 3, 5, or 7), copied from the
    /// stage at creation time so a later change to Stage.BestOf never
    /// retroactively changes an in-progress or decided series.
    /// </summary>
    public required int BestOf { get; set; }

    /// <summary>
    /// Set once one team has won the majority of the series' games.
    /// </summary>
    public Guid? WinningTeamId { get; set; }
    public Team? WinningTeam { get; set; }

    public virtual ICollection<Match> Matches { get; set; } = [];
}
