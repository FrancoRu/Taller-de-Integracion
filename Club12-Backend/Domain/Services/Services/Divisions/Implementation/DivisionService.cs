using Entities.DTOs.Abstract;
using Entities.DTOs.Divisions;
using Entities.Models.Divisions;
using Entities.Models.Matches;
using Entities.Models.Players;
using Entities.Models.PlayerStatistics;
using Entities.Models.Positions;
using Entities.Models.TopScorers;

using Microsoft.EntityFrameworkCore;

using Services.DataAccessLayer.GenericEntity;
using Services.Utils.OrderFiltering;

using System.Linq.Expressions;

namespace Services.Services.Divisions.Implementation;

public class DivisionService(IGenericService<Division> _genericDivisionService) : IDivisionService
{
    public async Task<Division> CreateDivisionAsync(Division divisionEntity)
    {
        await _genericDivisionService.InsertAsync(divisionEntity);
        return divisionEntity;
    }

    public async Task<bool> DeleteDivisionAsync(Division divisionEntity)
    {
        try
        {
            await _genericDivisionService.DeleteAsync(divisionEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateDivisionAsync(Division divisionEntity)
    {
        try
        {
            await _genericDivisionService.UpdateAsync(divisionEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<Division?> GetDivisionByIdAsync(Guid divisionId) => await _genericDivisionService.FilterByExpression(division => division.Id == divisionId)
                                 .Include(division => division.Teams)
                                 .Include(division => division.Matches)
                                    .ThenInclude(match => match.HomeTeam)
                                 .Include(division => division.Matches)
                                    .ThenInclude(match => match.VisitorTeam)
                                 .FirstOrDefaultAsync();

    public async Task<Division?> GetDivisionWithStatsAsync(Guid divisionId)
    {
        Division? division = await _genericDivisionService.FilterByExpression(division => division.Id == divisionId)
                                                .Include(division => division.Teams)
                                                .Include(division => division.Matches)
                                                    .ThenInclude(match => match.HomeTeam)
                                                .Include(division => division.Matches)
                                                    .ThenInclude(match => match.VisitorTeam)
                                                .FirstOrDefaultAsync();

        if (division is null)
        {
            return null;
        }

        List<Position> teamStats =
        [
            .. division.Teams.Select(team =>
            {
                IEnumerable<Match> homeMatches = division.Matches.Where(m => m.HomeTeamId == team.Id && m.IsFinished);
                IEnumerable<Match> visitorMatches = division.Matches.Where(m => m.VisitorTeamId == team.Id && m.IsFinished);

                int homeWins = homeMatches.Count(m => m.WinningTeamId == team.Id);
                int homeLosses = homeMatches.Count(m => m.WinningTeamId != team.Id && m.WinningTeamId != null);
                int visitorWins = visitorMatches.Count(m => m.WinningTeamId == team.Id);
                int visitorLosses = visitorMatches.Count(m => m.WinningTeamId != team.Id && m.WinningTeamId != null);

                int homePointsFor = homeMatches.Sum(m => m.HomeScore ?? 0);
                int homePointsAgainst = homeMatches.Sum(m => m.VisitorScore ?? 0);

                int visitorPointsFor = visitorMatches.Sum(m => m.VisitorScore ?? 0);
                int visitorPointsAgainst = visitorMatches.Sum(m => m.HomeScore ?? 0);

                int matchesPlayed = homeMatches.Count() + visitorMatches.Count();
                int wins = homeWins + visitorWins;
                int losses = homeLosses + visitorLosses;
                int pointsFor = homePointsFor + visitorPointsFor;
                int pointsAgainst = homePointsAgainst + visitorPointsAgainst;
                int points = (wins * 2) + losses;

                return new Position
                {
                    TeamId = team.Id,
                    TeamName = team.Name,
                    MatchesPlayed = matchesPlayed,
                    Wins = wins,
                    Losses = losses,
                    PointsFor = pointsFor,
                    PointsAgainst = pointsAgainst,
                    PointsDifference = pointsFor - pointsAgainst,
                    Points = points
                };
            })
            .OrderByDescending(p => p.Points)
            .ThenByDescending(p => p.PointsDifference)
            .ThenByDescending(p => p.PointsFor)
            .ThenByDescending(p => p.Wins)
        ];

        division.Positions = teamStats;
        return division;
    }

    public async Task<PaginatedResponse<Division>> GetAllDivisionsAsync(GetDivisionsFilteredRequest filter)
    {
        Expression<Func<Division, bool>> expression = QueryableExtensions.ConstructFilterExpression<Division, GetDivisionsFilteredRequest>(filter);
        IQueryable<Division> filteredDivisions = _genericDivisionService.FilterByExpressionWithPagination(expression, filter, division => division.Teams,
                                                                                                                              division => division.Matches)
                                                                                                                                         .SortBy(filter);
        int totalCount = await _genericDivisionService.GetCountAsync(expression);

        List<Division> items = await filteredDivisions.ToListAsync();

        return new PaginatedResponse<Division>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = items
        };
    }

    public async Task<List<TopScorer>?> GetTopScorersByDivisionAsync(Guid divisionId)
    {
        Division? division = await _genericDivisionService
                                .FilterByExpression(d => d.Id == divisionId)
                                .Include(d => d.Teams)
                                .Include(d => d.Matches)
                                    .ThenInclude(m => m.PlayerStatistics)
                                .Include(d => d.Matches)
                                    .ThenInclude(m => m.HomeTeam)
                                .Include(d => d.Matches)
                                    .ThenInclude(m => m.VisitorTeam)
                                .FirstOrDefaultAsync();

        if (division is null)
        {
            return null;
        }

        if (division.Matches.Count == 0)
        {
            return [];
        }

        int totalTeams = division.Teams.Count;
        int totalMatches = (totalTeams * 2) - 1;

        List<PlayerStatistic> playerStatistics = division.Matches
            .Where(m => m.IsFinished)
            .SelectMany(m => m.PlayerStatistics)
            .ToList();

        if (playerStatistics.Count == 0)
        {
            return [];
        }

        List<TopScorer> topScorers = playerStatistics
            .GroupBy(ps => ps.PlayerId)
            .Select(group =>
            {
                PlayerStatistic? firstStat = group.FirstOrDefault();
                Player? player = firstStat?.Player;

                Dictionary<int, int> weeklyScores = Enumerable.Range(1, totalMatches)
                    .ToDictionary(week => week, week => 0);

                foreach (PlayerStatistic? stat in group)
                {
                    int matchWeek = stat.Match?.MatchWeek ?? 0;
                    if (matchWeek > 0 && matchWeek <= totalMatches)
                    {
                        weeklyScores[matchWeek] += stat.Value;
                    }
                }

                return new TopScorer
                {
                    PlayerId = group.Key,
                    FirstName = player?.Names ?? "Unknown",
                    LastName = player?.LastName ?? "Unknown",
                    WeeklyScores = weeklyScores
                };
            })
            .OrderByDescending(ts => ts.TotalPoints)
            .Take(10)
            .ToList();

        return topScorers;
    }
}
