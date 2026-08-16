using System.Collections.Generic;
using System.Linq;
using Domain.Entities.Models;

namespace Application.Utils.Helper.Standings;

/// <summary>
/// Computes division standings from a set of matches. Basketball has no
/// draws: every finished, fully-seeded match has exactly one winner, so a
/// team either wins (2 points) or loses (1 point) — never a tie.
/// </summary>
public static class PositionCalculator
{
    private const int PointsForWin = 2;
    private const int PointsForLoss = 1;

    /// <summary>
    /// Builds one Position per team that appears in at least one finished,
    /// fully-seeded match. Unfinished matches and matches missing a home
    /// or visitor team (not yet seeded) are ignored.
    /// </summary>
    public static List<Position> CalculatePositions(IEnumerable<Match> matches)
    {
        Dictionary<System.Guid, Position> positionsByTeamId = [];

        foreach (Match match in matches)
        {
            if (!match.IsFinished || match.HomeTeam is null || match.VisitorTeam is null
                || match.HomeScore is null || match.VisitorScore is null)
            {
                continue;
            }

            bool homeWon = match.WinningTeamId == match.HomeTeamId;

            ApplyResult(positionsByTeamId, match.HomeTeam, match.HomeScore.Value, match.VisitorScore.Value, homeWon);
            ApplyResult(positionsByTeamId, match.VisitorTeam, match.VisitorScore.Value, match.HomeScore.Value, !homeWon);
        }

        return positionsByTeamId.Values
            .Select(position =>
            {
                position.PointsDifference = position.PointsFor - position.PointsAgainst;
                return position;
            })
            .OrderByDescending(position => position.Points)
            .ThenByDescending(position => position.PointsDifference)
            .ThenByDescending(position => position.PointsFor)
            .ToList();
    }

    private static void ApplyResult(
        Dictionary<System.Guid, Position> positionsByTeamId,
        Team team,
        int pointsFor,
        int pointsAgainst,
        bool won)
    {
        if (!positionsByTeamId.TryGetValue(team.Id, out Position? position))
        {
            position = new Position
            {
                TeamId = team.Id,
                TeamName = team.Name,
                LogoUrl = team.LogoUrl,
                MatchesPlayed = 0,
                Wins = 0,
                Losses = 0,
                PointsFor = 0,
                PointsAgainst = 0,
                PointsDifference = 0,
                Points = 0,
            };
            positionsByTeamId[team.Id] = position;
        }

        position.MatchesPlayed += 1;
        position.PointsFor += pointsFor;
        position.PointsAgainst += pointsAgainst;

        if (won)
        {
            position.Wins += 1;
            position.Points += PointsForWin;
        }
        else
        {
            position.Losses += 1;
            position.Points += PointsForLoss;
        }
    }
}
