using System;
using System.Linq;
using Domain.Entities.Models;

namespace Application.Utils.Helper.Series;

/// <summary>
/// Determines whether a best-of-N playoff series has been decided yet.
/// </summary>
public static class SeriesDecisionCalculator
{
    /// <summary>
    /// Returns the winning team's id once one team has won more than half
    /// of the series' BestOf games, based on the finished games recorded so
    /// far. Returns null while the series is still undecided.
    /// </summary>
    public static Guid? DetermineWinner(MatchSeries series)
    {
        int winsNeeded = series.BestOf / 2 + 1;

        int homeWins = series.Matches.Count(game => game.IsFinished && game.WinningTeamId == series.HomeTeamId);
        if (homeWins >= winsNeeded)
        {
            return series.HomeTeamId;
        }

        int visitorWins = series.Matches.Count(game => game.IsFinished && game.WinningTeamId == series.VisitorTeamId);
        if (visitorWins >= winsNeeded)
        {
            return series.VisitorTeamId;
        }

        return null;
    }
}
