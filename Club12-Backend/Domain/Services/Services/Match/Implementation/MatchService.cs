using Entities.DTOs.Abstract;
using Entities.DTOs.Match;
using Entities.Models.MatchEntity;
using Entities.Models.PlayoffSeriesEntity;
using Entities.Models.ScorerModel;
using Entities.Models.TeamEntity;

using Microsoft.EntityFrameworkCore;

using Services.DataAccessLayer.GenericEntity;
using Services.Utils.OrderFiltering;

using System.Linq.Expressions;

using MatchType = Entities.Models.MatchTypeEnum.MatchType;

namespace Services.Services.MatchService.Implementation;

public class MatchService(IGenericService<Match> _genericMatchService) : IMatchService
{
    public async Task<Match> CreateMatchAsync(Match matchEntity)
    {
        await _genericMatchService.InsertAsync(matchEntity);
        return matchEntity;
    }

    public async Task<Match?> GetMatchByIdAsync(Guid matchId) => await _genericMatchService.FilterByExpression(match => match.Id == matchId)
            .Include(m => m.HomeTeam)
            .Include(m => m.VisitorTeam)
            .FirstOrDefaultAsync();

    public async Task<Match?> GetMatchByIdWithScorersAsync(Guid matchId)
    {
        Match? match = await _genericMatchService.FilterByExpression(match => match.Id == matchId)
            .Include(m => m.HomeTeam)
            .Include(m => m.VisitorTeam)
            .Include(m => m.PlayerStatistics)
                .ThenInclude(ps => ps.Player)
            .Include(m => m.Venue)
            .FirstOrDefaultAsync();

        if (match is null)
        {
            return null;
        }

        var playerStats = match.PlayerStatistics
            .Select(ps => new
            {
                ps.PlayerId,
                ps.Player.Names,
                ps.Player.LastName,
                ps.Value,
                ps.Player.TeamId,
                TeamName = ps.Player.Team.Name,
                MatchHomeTeamId = match.HomeTeamId,
                MatchVisitorTeamId = match.VisitorTeamId
            })
            .OrderBy(ps => ps.Value)
            .ToList();

        List<Scorer> homeScorers =
        [
            .. playerStats
                        .Where(ps => ps.TeamId == match.HomeTeamId)
                        .Select(ps => new Scorer
                        {
                            PlayerId = ps.PlayerId,
                            Names = ps.Names,
                            LastName = ps.LastName,
                            Points = ps.Value,
                            TeamId = match.HomeTeamId.Value,
                            TeamName = match.HomeTeam.Name
                        })
                        .OrderByDescending(ps => ps.Points)
        ];

        List<Scorer> awayScorers =
        [
            .. playerStats
                        .Where(ps => ps.TeamId == match.VisitorTeamId)
                        .Select(ps => new Scorer
                        {
                            PlayerId = ps.PlayerId,
                            Names = ps.Names,
                            LastName = ps.LastName,
                            Points = ps.Value,
                            TeamId = match.VisitorTeamId,
                            TeamName = match.VisitorTeam.Name
                        })
                        .OrderByDescending(ps => ps.Points)
        ];

        match.HomeScorers = homeScorers;
        match.VisitorScorers = awayScorers;

        return match;
    }

    public async Task<bool> DeleteMatchAsync(Match matchEntity)
    {
        try
        {
            await _genericMatchService.DeleteAsync(matchEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateMatchAsync(Match matchEntity)
    {
        try
        {
            await _genericMatchService.UpdateAsync(matchEntity);
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
        IQueryable<Match> filteredMatches = _genericMatchService.FilterByExpressionWithPagination(expression, filter, match => match.HomeTeam,
                                                                                                                      match => match.VisitorTeam,
                                                                                                                      match => match.Division,
                                                                                                                      match => match.Venue)
                                                                                                                        .SortBy(filter);

        int totalCount = await _genericMatchService.GetCountAsync(expression);

        return new PaginatedResponse<Match>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = await filteredMatches.ToListAsync()
        };
    }

    public async Task GenerateFixtureAsync(Guid divisionId, IEnumerable<Team> teams)
    {
        if (teams is null || !teams.Any() || teams.Count() < 2 || teams.Count() % 2 != 0)
        {
            throw new ArgumentException("The list must have at least two teams and an even number of teams to generate a fixture.");
        }

        int numberOfTeams = teams.Count();
        int numberOfRounds = (numberOfTeams - 1) * 2;
        DateTime currentMatchDate = DateTime.UtcNow;

        List<List<(Team home, Team away)>> firstHalfRounds = Enumerable.Range(0, numberOfRounds / 2)
            .Select(round =>
            {
                List<Team> rotatedTeams = teams.Skip(round).Concat(teams.Take(round)).ToList();
                return rotatedTeams
                    .Take(numberOfTeams / 2)
                    .Zip(rotatedTeams.Skip(numberOfTeams / 2).Reverse(), (home, away) => (home, away))
                    .ToList();
            }).ToList();

        List<Match> firstRoundMatches = firstHalfRounds
            .SelectMany((roundMatches, roundIndex) => roundMatches
                .Select(match => new Match
                {
                    HomeTeamId = match.home.Id,
                    VisitorTeamId = match.away.Id,
                    HomeTeam = match.home,
                    VisitorTeam = match.away,
                    Type = MatchType.Regular,
                    MatchWeek = roundIndex + 1,
                    IsFinished = false,
                    DivisionId = divisionId,
                    MatchDate = currentMatchDate
                }))
            .ToList();

        currentMatchDate = currentMatchDate.AddDays(7 * firstRoundMatches.Count);

        List<Match> secondRoundMatches = firstHalfRounds
            .SelectMany((roundMatches, roundIndex) => roundMatches
                .Select(match => new Match
                {
                    HomeTeamId = match.away.Id,
                    VisitorTeamId = match.home.Id,
                    HomeTeam = match.away,
                    VisitorTeam = match.home,
                    Type = MatchType.Regular,
                    MatchWeek = roundIndex + 8,
                    IsFinished = false,
                    DivisionId = divisionId,
                    MatchDate = currentMatchDate
                }))
            .ToList();

        List<Match> allMatches = [.. firstRoundMatches, .. secondRoundMatches];

        await _genericMatchService.InsertRangeAsync(allMatches);
    }

    public async Task GeneratePlayoffMatchesAsync(Guid divisionId, IEnumerable<Team> teams, List<PlayoffSeries> playoffSeries)
    {
        DateTime currentMatchDate = DateTime.UtcNow;

        List<List<Team>> pairedTeams = PairTeams(teams);

        List<Match> playoffMatches = GenerateAllMatches(playoffSeries, pairedTeams, divisionId, currentMatchDate);

        await _genericMatchService.InsertRangeAsync(playoffMatches);
    }

    /// <summary>
    /// Pairs teams for the first round (Quarterfinals) based on seeding.
    /// </summary>
    private static List<List<Team>> PairTeams(IEnumerable<Team> teams)
    {
        if (teams.Count() % 2 != 0)
        {
            throw new ArgumentException("The number of teams must be even to generate playoff matches.");
        }

        // Order teams by seed (1 to 8)
        var orderedTeams = teams.OrderBy(t => t.Seed).ToList();

        // Pair teams: 1 vs. 8, 2 vs. 7, 3 vs. 6, 4 vs. 5
        var pairedTeams = new List<List<Team>>();
        for (int i = 0; i < orderedTeams.Count / 2; i++)
        {
            pairedTeams.Add(
        [
            orderedTeams[i],
            orderedTeams[orderedTeams.Count - 1 - i]
        ]);
        }

        return pairedTeams;
    }

    /// <summary>
    /// Generates all matches for a given series and team pair.
    /// </summary>
    private static List<Match> GenerateAllMatches(List<PlayoffSeries> playoffSeries, List<List<Team>> pairedTeams, Guid divisionId, DateTime startDate)
    {
        List<Match> allMatches = new();

        foreach (PlayoffSeries series in playoffSeries)
        {
            foreach (List<Team> teamPair in pairedTeams)
            {
                List<Match> matches = GenerateMatchesForSeries(series, teamPair, divisionId, startDate);
                allMatches.AddRange(matches);
            }

            pairedTeams = pairedTeams
                .Select(pair => new List<Team> { pair[0], pair[1] })
                .ToList();
        }

        return allMatches;
    }

    /// <summary>
    /// Generates matches for a given series and team pair.
    /// </summary>
    private static List<Match> GenerateMatchesForSeries(PlayoffSeries series, List<Team> teamPair, Guid divisionId, DateTime startDate)
    {
        List<Match> matches = new();

        Team homeTeam = teamPair[0];
        Team awayTeam = teamPair[1];

        for (int gameNumber = 1; gameNumber <= 3; gameNumber++)
        {
            Match match = new()
            {
                HomeTeamId = homeTeam.Id,
                VisitorTeamId = awayTeam.Id,
                HomeTeam = homeTeam,
                VisitorTeam = awayTeam,
                Type = MatchType.Playoff,
                IsFinished = false,
                DivisionId = divisionId,
                MatchDate = startDate.AddDays((gameNumber - 1) * 2),
                PlayoffSeriesId = series.Id,
                PlayoffSeries = series,
                GameNumber = gameNumber
            };

            matches.Add(match);
        }

        return matches;
    }
}
