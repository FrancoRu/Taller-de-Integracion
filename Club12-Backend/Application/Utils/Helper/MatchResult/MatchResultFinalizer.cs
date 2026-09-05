using Application.Utils.Constants;

using Domain.Entities.Models;
using Domain.Enums;

using System;

namespace Application.Utils.Helper.MatchResult;

/// <summary>
/// Single source of truth for turning a home/visitor score pair into a finished match.
/// </summary>
public static class MatchResultFinalizer
{
    /// <summary>
    /// Applies a decisive final result to match, deriving the winning team from whichever score is higher.
    /// </summary>
    /// <param name="match">The match to finalize. Its Stage, HomeTeam, HomeTeamId and their visitor counterparts must be loaded.</param>
    /// <param name="homeScore">The home team's final score.</param>
    /// <param name="visitorScore">The visitor team's final score.</param>
    /// <exception cref="InvalidOperationException">Thrown when the score is tied.</exception>
    public static void ApplyResult(Match match, int homeScore, int visitorScore)
    {
        ArgumentNullException.ThrowIfNull(match);

        if (homeScore == visitorScore)
        {
            throw new InvalidOperationException(
                match.Stage.StageType == StageType.Group
                    ? ErrorMessages.Match.GroupStageTieNotAllowed
                    : ErrorMessages.Match.PlayoffTieNotAllowed);
        }

        match.HomeScore = homeScore;
        match.VisitorScore = visitorScore;

        bool homeWon = homeScore > visitorScore;
        match.WinningTeam = homeWon ? match.HomeTeam : match.VisitorTeam;
        match.WinningTeamId = homeWon ? match.HomeTeamId : match.VisitorTeamId;

        match.IsFinished = true;
        match.Status = MatchStatus.Played;
    }
}
