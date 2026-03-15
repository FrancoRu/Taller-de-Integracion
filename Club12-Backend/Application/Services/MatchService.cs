using Application.DTOs.Abstract.Response;
using Application.DTOs.Match.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Extensions;
using Domain.Entities.Models;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MatchType = Domain.Enums.MatchType;

namespace Application.Services;

public class MatchService(IUnitOfWork unitOfWork) : IMatchService
{
    private readonly IMatchRepository matchRepository = unitOfWork.MatchRepository;
    private readonly IStageRepository stageRepository = unitOfWork.StageRepository;

    public async Task<Match> CreateMatchAsync(Match matchEntity)
    {
        await matchRepository.AddAsync(matchEntity);
        return matchEntity;
    }

    public async Task<Match?> GetMatchByIdAsync(Guid matchId)
        => await matchRepository.GetByIdAsync(matchId);

    public async Task<Match?> GetMatchByIdWithScorersAsync(Guid matchId)
    {
        Match? match = await matchRepository.GetByIdAsync(matchId,
            includes: [m => m.HomeTeam!,
                        m => m.VisitorTeam!,
                        m => m.PlayerStatistics,
                        m => m.Venue!]
            );

        if (match is null)
        {
            return null;
        }

        var playerStats = match.PlayerStatistics
            .Select(ps => new
            {
                ps.PlayerId,
                ps.Player.FirstName,
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
                            Names = ps.FirstName,
                            LastName = ps.LastName,
                            Points = ps.Value,
                            TeamId = match.HomeTeamId ?? throw new MissingFieldException(""),
                            TeamName = match.HomeTeam?.Name ?? throw new MissingFieldException("")
                        })
                        .OrderByDescending(ps => ps.Points)
        ];

        List<Scorer> awayScorers =
        [
            //.. playerStats
            //            .Where(ps => ps.TeamId == match.VisitorTeamId)
            //            .Select(ps => new Scorer
            //            {
            //                PlayerId = ps.PlayerId,
            //                Names = ps.FirstName,
            //                LastName = ps.LastName,
            //                Points = ps.Value,
            //                TeamId = match.VisitorTeamId,
            //                TeamName = match.VisitorTeam.Name
            //            })
            //            .OrderByDescending(ps => ps.Points)
        ];

        match.HomeScorers = homeScorers;
        match.VisitorScorers = awayScorers;

        return match;
    }

    public async Task DeleteMatchAsync(Guid id)
        => await matchRepository.RemoveAsync(match => match.Id == id);

    public async Task UpdateMatchAsync(Match matchEntity) =>
        await matchRepository.UpdateAsync(matchEntity);

    public async Task<PaginatedResponse<Match>> GetAllMatchesAsync(GetMatchesFilteredRequest filter)
    {
        Expression<Func<Match, bool>> expression = QueryableExtensions.ConstructFilterExpression<Match, GetMatchesFilteredRequest>(filter);
        IEnumerable<Match> filteredMatches = await matchRepository.FindAsync(expression, filter: filter, includes: [match => match.HomeTeam!,
                                                                                                                      match => match.VisitorTeam!,
                                                                                                                      match => match.Venue!]);

        int totalCount = await matchRepository.CountAsync(expression);

        return new PaginatedResponse<Match>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = filteredMatches
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

        List<List<(Team home, Team away)>> firstHalfRounds = [.. Enumerable.Range(0, numberOfRounds / 2)
            .Select(round =>
            {
                List<Team> rotatedTeams = [.. teams.Skip(round), .. teams.Take(round)];
                return rotatedTeams
                    .Take(numberOfTeams / 2)
                    .Zip(rotatedTeams.Skip(numberOfTeams / 2).Reverse(), (home, away) => (home, away))
                    .ToList();
            })];

        List<Match> firstRoundMatches = [.. firstHalfRounds
            .SelectMany((roundMatches, roundIndex) => roundMatches
                .Select(match => new Match
                {
                    HomeTeamId = match.home.Id,
                    VisitorTeamId = match.away.Id,
                    HomeTeam = match.home,
                    VisitorTeam = match.away,
                    Type = MatchType.Regular,
                    IsFinished = false,
                    MatchDate = currentMatchDate,
                    CreatedBy = "System",
                }))];

        currentMatchDate = currentMatchDate.AddDays(7 * firstRoundMatches.Count);

        List<Match> secondRoundMatches = [.. firstHalfRounds
            .SelectMany((roundMatches, roundIndex) => roundMatches
                .Select(match => new Match
                {
                    HomeTeamId = match.away.Id,
                    VisitorTeamId = match.home.Id,
                    HomeTeam = match.away,
                    VisitorTeam = match.home,
                    Type = MatchType.Regular,
                    IsFinished = false,
                    MatchDate = currentMatchDate,
                    CreatedBy = "System",
                }))];

        List<Match> allMatches = [.. firstRoundMatches, .. secondRoundMatches];

        await matchRepository.AddRangeAsync(allMatches);
    }

    public async Task<List<Match>> CreateAutomatedMatchesAsync(Guid stageId)
    {
        Stage stage = await stageRepository.GetByIdAsync(stageId)
            ?? throw new InvalidOperationException("Stage not found.");

        if (stage.Matches.Count > 0)
        {
            throw new InvalidOperationException("Cannot process the current request because the current stage already has some matches.");
        }

        List<Match> matches = stage.StageType switch
        {
            StageType.Group => await CreateGroupStageMatchesAsync(stage),
            StageType.QuarterFinal => await CreateKnockoutStageMatchesAsync(stage),
            StageType.SemiFinal => await CreateKnockoutStageMatchesAsync(stage),
            StageType.ThirdPlace => await CreateFinalStageMatchesAsync(stage),
            StageType.Final => await CreateFinalStageMatchesAsync(stage),
            _ => throw new NotSupportedException("Stage type not supported for automated match creation.")
        };


        await matchRepository.AddRangeAsync(matches);
        return matches;
    }

    private static Match BuildMatch(Stage stage, DateTime matchDate, MatchType matchType = MatchType.Playoff) =>
        new()
        {
            StageId = stage.Id,
            Type = matchType,
            IsFinished = false,
            MatchDate = matchDate,
            CreatedBy = "System"
        };

    private static async Task<List<Match>> CreateGroupStageMatchesAsync(Stage stage, int totalTeams = 4)
    {
        List<Match> matches = [];

        int totalMatches = totalTeams * (totalTeams - 1) / 2;
        List<DateTime> matchDates = DistributeMatchDates(stage.StartDate, stage.EndDate, totalMatches);

        for (int i = 0; i < totalMatches; i++)
        {
            matches.Add(BuildMatch(stage, matchDates[i], MatchType.Regular));
        }

        return matches;
    }

    private static async Task<List<Match>> CreateKnockoutStageMatchesAsync(Stage stage)
    {
        List<Match> matches = [];
        int matchCount = stage.StageType switch
        {
            StageType.QuarterFinal => 4,
            StageType.SemiFinal => 2,
            _ => throw new InvalidOperationException("Invalid knockout stage type.")
        };

        List<DateTime> matchDates = DistributeMatchDates(stage.StartDate, stage.EndDate, matchCount);

        for (int i = 0; i < matchCount; i++)
        {
            matches.Add(BuildMatch(stage, matchDates[i]));
        }

        return matches;
    }

    private static async Task<List<Match>> CreateFinalStageMatchesAsync(Stage stage)
    {
        List<Match> matches = [];
        List<DateTime> matchDates = DistributeMatchDates(stage.StartDate, stage.EndDate, 1);

        matches.Add(BuildMatch(stage, matchDates[0]));

        return matches;
    }

    private static List<DateTime> DistributeMatchDates(DateTime startDate, DateTime endDate, int matchCount)
    {
        if (matchCount <= 0)
        {
            throw new ArgumentException("Match count must be greater than zero.", nameof(matchCount));
        }

        if (endDate < startDate)
        {
            throw new ArgumentException("End date must be after start date.");
        }

        List<DateTime> matchDates = [];

        if (matchCount == 1)
        {
            matchDates.Add(startDate.AddDays((endDate - startDate).TotalDays / 2));
            return matchDates;
        }

        double totalDays = (endDate - startDate).TotalDays;
        double interval = totalDays / (matchCount - 1);

        for (int i = 0; i < matchCount; i++)
        {
            DateTime matchDate = startDate.AddDays(interval * i);
            matchDates.Add(matchDate);
        }

        return matchDates;
    }

    
}
