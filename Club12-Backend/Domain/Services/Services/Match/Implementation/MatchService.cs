using Entities.DTOs.Abstract;
using Entities.DTOs.Match;
using Entities.Models.DivisionEntity;
using Entities.Models.MatchEntity;
using Entities.Models.TeamEntity;

using Microsoft.EntityFrameworkCore;

using Services.DataAccessLayer.GenericEntity;
using Services.Utils.OrderFiltering;

using System.Linq.Expressions;

using MatchType = Entities.Models.MatchTypeEnum.MatchType;


namespace Services.Services.MatchService.Implementation;

public class MatchService(IGenericService<Match> genericMatchService) : IMatchService
{
    public Match CreateMatch(Match matchEntity)
    {
        genericMatchService.Insert(matchEntity);
        return matchEntity;
    }

    public Match? GetMatchById(Guid matchId)
    {
        return genericMatchService.FilterByExpression(match => match.Id == matchId)
                                  .Include(match => match.HomeTeam)
                                  .Include(match => match.VisitorTeam)
                                  .Include(match => match.Division)
                                  .Include(match => match.Venue)
                                  .FirstOrDefault();
    }

    public void DeleteMatch(Match matchEntity)
    {
        genericMatchService.Delete(matchEntity);
    }

    public async Task<bool> UpdateMatchAsync(Match matchEntity)
    {
        try
        {
            await genericMatchService.UpdateAsync(matchEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<PaginatedResponse<Match>> GetAllMatchesAsync(GetMatchesFilteredRequest filter)
    {
        Expression<Func<Match, bool>> expression = QueryableExtensions.ConstructFilterExpression<Match, GetMatchesFilteredRequest>(filter);
        IQueryable<Match> filteredMatches = genericMatchService.FilterByExpressionWithPagination(expression, filter, match => match.HomeTeam,
                                                                                                                     match => match.VisitorTeam,
                                                                                                                     match => match.Division,
                                                                                                                     match => match.Venue)
                                                                                                .SortBy(filter);
        int totalCount = await genericMatchService.GetCountAsync(expression);

        return new PaginatedResponse<Match>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = await filteredMatches.ToListAsync()
        };
    }

    /// <summary>
    /// Generates the fixture (matches) for the given division.
    /// </summary>
    /// <param name="division">The division for which the fixture should be generated.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task GenerateFixtureAsync(Division division)
    {
        if (division.Teams is null || division.Teams.Count is 0 || division.Teams.Count < 2 || division.Teams.Count % 2 is not 0)
        {
            throw new ArgumentException("The division must have at least two teams to generate a fixture.");
        }

        List<Team> teams = [.. division.Teams];

        int numberOfRounds = teams.Count - 1;

        List<Match> matches = [];
        DateTime currentMatchDate = DateTime.UtcNow;

        var matchups = teams
            .SelectMany((homeTeam, homeIndex) => teams
                .Where((awayTeam, awayIndex) => awayIndex > homeIndex)
                .Select(awayTeam => new { homeTeam, awayTeam }))
            .ToList();

        foreach (int round in Enumerable.Range(0, numberOfRounds))
        {
            List<Match> roundMatches = matchups
                .Where((matchup, index) => index % numberOfRounds == round)
                .Select((matchup, index) =>
                {
                    Team homeTeam = matchup.homeTeam;
                    Team awayTeam = matchup.awayTeam;

                    Match match = new()
                    {
                        HomeTeamId = homeTeam.Id,
                        VisitorTeamId = awayTeam.Id,
                        HomeTeam = homeTeam,
                        VisitorTeam = awayTeam,
                        Type = MatchType.Regular,
                        MatchWeek = round + 1,
                        IsFinished = false,
                        DivisionId = division.Id,
                        Division = division,
                        MatchDate = currentMatchDate
                    };

                    currentMatchDate = currentMatchDate.AddDays(7);
                    return match;
                }).ToList();

            matches.AddRange(roundMatches);
        }

        await genericMatchService.InsertRangeAsync(matches);
    }

}
