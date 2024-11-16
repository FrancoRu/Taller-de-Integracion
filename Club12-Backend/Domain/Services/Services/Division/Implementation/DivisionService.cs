using Entities.DTOs.Abstract;
using Entities.DTOs.Division;
using Entities.Models.DivisionEntity;
using Entities.Models.MatchEntity;
using Entities.Models.PositionModel;

using Microsoft.EntityFrameworkCore;

using Services.DataAccessLayer.GenericEntity;
using Services.Utils.OrderFiltering;

using System.Linq.Expressions;

namespace Services.Services.DivisionService.Implementation;

public class DivisionService(IGenericService<Division> genericDivisionService) : IDivisionService
{
    public Division CreateDivision(Division divisionEntity)
    {
        genericDivisionService.Insert(divisionEntity);
        return divisionEntity;
    }

    public void DeleteDivision(Division divisionEntity)
    {
        genericDivisionService.Delete(divisionEntity);
    }

    public async Task<bool> UpdateDivisionAsync(Division divisionEntity)
    {
        try
        {
            await genericDivisionService.UpdateAsync(divisionEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public Division? GetDivisionById(Guid divisionId)
    {
        return genericDivisionService.FilterByExpression(division => division.Id == divisionId)
                                 .Include(division => division.Teams)
                                 .Include(division => division.Matches)
                                    .ThenInclude(match => match.HomeTeam)
                                 .Include(division => division.Matches)
                                    .ThenInclude(match => match.VisitorTeam)
                                 .FirstOrDefault();
    }

    public Division? GetDivisionWithStats(Guid divisionId)
    {
        Division? division = genericDivisionService.FilterByExpression(division => division.Id == divisionId)
                                                .Include(division => division.Teams)
                                                .Include(division => division.Matches)
                                                    .ThenInclude(match => match.HomeTeam)
                                                .Include(division => division.Matches)
                                                    .ThenInclude(match => match.VisitorTeam)
                                                .FirstOrDefault();

        if (division == null) return null;

        List<Position> teamStats =
        [
            .. division.Teams.Select(team =>
                    {
                        IEnumerable<Match> homeMatches = division.Matches.Where(m => m.HomeTeamId == team.Id && m.IsFinished);
                        IEnumerable<Match> visitorMatches = division.Matches.Where(m => m.VisitorTeamId == team.Id && m.IsFinished);

                        int homeWins = homeMatches.Count(m => m.HomeScore > m.VisitorScore);
                        int homeLosses = homeMatches.Count(m => m.HomeScore < m.VisitorScore);
                        int homePointsFor = homeMatches.Sum(m => m.HomeScore ?? 0);
                        int homePointsAgainst = homeMatches.Sum(m => m.VisitorScore ?? 0);

                        int visitorWins = visitorMatches.Count(m => m.VisitorScore > m.HomeScore);
                        int visitorLosses = visitorMatches.Count(m => m.VisitorScore < m.HomeScore);
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
                    .ThenByDescending(p => p.Wins),
        ];

        division.Positions = teamStats;
        return division;
    }

    public async Task<PaginatedResponse<Division>> GetAllDivisionsAsync(GetDivisionsFilteredRequest filter)
    {
        Expression<Func<Division, bool>> expression = QueryableExtensions.ConstructFilterExpression<Division, GetDivisionsFilteredRequest>(filter);
        IQueryable<Division> filteredDivisions = genericDivisionService.FilterByExpressionWithPagination(expression, filter, division => division.Teams,
                                                                                                                             division => division.Matches)
                                                                                                                                         .SortBy(filter);
        int totalCount = await genericDivisionService.GetCountAsync(expression);

        return new PaginatedResponse<Division>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = await filteredDivisions.ToListAsync()
        };
    }
}
