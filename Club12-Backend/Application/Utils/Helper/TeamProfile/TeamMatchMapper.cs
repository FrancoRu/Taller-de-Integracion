using Application.DTOs.Team.Response;

using Domain.Entities.Models;

using System;

namespace Application.Utils.Helper.TeamProfile;

/// <summary>
/// Pure projection of a <see cref="Match"/> into a
/// <see cref="TeamMatchResponse"/> oriented from one team's perspective:
/// scores, opponent, home/away flag and the win/loss result are all resolved
/// relative to <c>teamId</c> rather than to the raw home/visitor sides. Kept
/// side-effect free so it can be unit-tested without a database.
/// </summary>
public static class TeamMatchMapper
{
    /// <summary>
    /// Projects <paramref name="match"/> from the point of view of the team
    /// identified by <paramref name="teamId"/>. The team is assumed to be one
    /// of the match's two sides (the caller filters matches to this team).
    /// </summary>
    /// <param name="match">The match to project. Its HomeTeam/VisitorTeam/Venue
    /// navigations should be loaded for names and logos to be populated.</param>
    /// <param name="teamId">The team whose perspective to orient the result to.</param>
    /// <returns>The team-oriented match projection.</returns>
    public static TeamMatchResponse Map(Match match, Guid teamId)
    {
        bool isHome = match.HomeTeamId == teamId;

        Domain.Entities.Models.Team? opponent = isHome ? match.VisitorTeam : match.HomeTeam;
        Guid? opponentId = isHome ? match.VisitorTeamId : match.HomeTeamId;
        int? teamScore = isHome ? match.HomeScore : match.VisitorScore;
        int? opponentScore = isHome ? match.VisitorScore : match.HomeScore;

        // Basketball has no draws: a finished match always has a winning team
        // (a walkover sets it too), so "W" iff this team is the winner, else "L".
        string? result = null;
        if (match.IsFinished)
        {
            result = match.WinningTeamId == teamId ? "W" : "L";
        }

        return new TeamMatchResponse
        {
            MatchId = match.Id,
            MatchDate = match.MatchDate,
            IsFinished = match.IsFinished,
            Status = match.Status.ToString(),
            IsHome = isHome,
            OpponentTeamId = opponentId ?? Guid.Empty,
            OpponentName = opponent?.Name ?? string.Empty,
            OpponentLogoUrl = opponent?.LogoUrl,
            TeamScore = teamScore,
            OpponentScore = opponentScore,
            Result = result,
            VenueName = match.Venue?.Name,
        };
    }
}
