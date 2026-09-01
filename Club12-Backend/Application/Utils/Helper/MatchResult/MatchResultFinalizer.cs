using Application.Utils.Constants;

using Domain.Entities.Models;
using Domain.Enums;

using System;

namespace Application.Utils.Helper.MatchResult;

/// <summary>
/// Single source of truth for turning a home/visitor score pair into a
/// finished match (HU-69/HU-70): basketball has no draws, so a tied score is
/// rejected with a stage-appropriate message instead of silently picking a
/// winner. Shared by every way a result can be loaded — typed directly
/// (<see cref="Application.Services.MatchService.LoadMatchResultAsync"/>) or
/// derived from both teams' player scoring sheets (HU-71/HU-72) — so they can
/// never disagree on how a winner is determined.
/// </summary>
public static class MatchResultFinalizer
{
    /// <summary>
    /// Applies a decisive final result to <paramref name="match"/>: sets its
    /// score, derives and sets the winning team from whichever score is
    /// higher, and marks it <see cref="Domain.Entities.Models.Match.IsFinished"/>
    /// / <see cref="MatchStatus.Played"/>. Mutates the entity in place; the
    /// caller is responsible for persisting it.
    /// </summary>
    /// <param name="match">The match to finalize. Its Stage, HomeTeam and HomeTeamId (and their visitor counterparts) must be loaded.</param>
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
