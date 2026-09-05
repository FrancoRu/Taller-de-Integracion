using Application.DTOs.Team.Response;

using Domain.Entities.Models;

using System;

namespace Application.Utils.Helper.TeamProfile;

/// <summary>
/// Pure projection of a Match into a TeamMatchResponse oriented from one team's perspective.
/// </summary>
public static class TeamMatchMapper
{
    /// <summary>
    /// Projects match from the point of view of the team identified by teamId.
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

        // Basketball has no draws, a finished match always has a winning team since a walkover sets one too, so the result is W when this team wins and L otherwise.
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
