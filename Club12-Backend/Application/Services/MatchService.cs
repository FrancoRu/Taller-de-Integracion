using Application.DTOs.Abstract.Response;
using Application.DTOs.Match.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Constants;
using Application.Utils.Constants.Stage;
using Application.Utils.Extensions;
using Application.Utils.Helper.RoundRobin;

using Domain.Constants;
using Domain.Entities.Models;
using Domain.Enums;

using LinqKit;

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
    private readonly ITeamRepository teamRepository = unitOfWork.TeamRepository;
    private readonly IStageTeamMatchRepository stageTeamMatchRepository = unitOfWork.StageTeamMatchRepository;

    public async Task<Match> CreateMatchAsync(Match matchEntity)
    {
        await matchRepository.AddAsync(matchEntity);
        return matchEntity;
    }

    public async Task<Match?> GetMatchByIdAsync(Guid matchId)
    {
        return await matchRepository.GetByIdAsync(matchId, includes: [m => m.HomeTeam!, m => m.VisitorTeam!]);
    }

    public async Task<Match?> GetMatchByIdWithScorersAsync(Guid matchId)
    {
        Match? match = await matchRepository.GetByIdAsync(matchId,
            includes: [m => m.HomeTeam!,
                        m => m.VisitorTeam!,
                        m => m.PlayerStatistics,
                        m => m.Venue!]
            );

        return match is null ? null : match;
    }

    public async Task DeleteMatchAsync(Guid id)
    {
        await matchRepository.RemoveAsync(match => match.Id == id);
    }

    public async Task UpdateMatchAsync(Match matchEntity)
    {
        await matchRepository.UpdateAsync(matchEntity);
    }

    public async Task<PaginatedResponse<Match>> GetAllMatchesAsync(GetMatchesFilteredRequest filter)
    {
        Expression<Func<Match, bool>> expression = QueryableExtensions.ConstructFilterExpression<Match, GetMatchesFilteredRequest>(filter);

        if (filter.DivisionId.HasValue)
        {
            Expression<Func<Match, bool>> divisionExpression = match => match.Stage.DivisionId == filter.DivisionId.Value;
            expression = expression.And(divisionExpression);

        }
        if (filter.TournamentId.HasValue)
        {
            Expression<Func<Match, bool>> tournamentExpression = match => match.Stage.Division.TournamentId == filter.TournamentId.Value;
            expression = expression.And(tournamentExpression);
        }
        if (!string.IsNullOrWhiteSpace(filter.HomeTeamName))
        {
            Expression<Func<Match, bool>> homeTeamExpression = match => match.HomeTeam != null && match.HomeTeam.Name.ToLower().Contains(filter.HomeTeamName.ToLower());
            expression = expression.And(homeTeamExpression);
        }
        if (!string.IsNullOrWhiteSpace(filter.VisitorTeamName))
        {
            Expression<Func<Match, bool>> visitorTeamExpression = match => match.VisitorTeam != null && match.VisitorTeam.Name.ToLower().Contains(filter.VisitorTeamName.ToLower());
            expression = expression.And(visitorTeamExpression);
        }

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

    public async Task<List<Match>> CreateAutomatedMatchesAsync(Guid stageId)
    {
        Stage stage = await stageRepository.GetByIdAsync(stageId, includes: [s => s.Matches, s => s.Division])
            ?? throw new InvalidOperationException(ErrorMessages.Stage.NotFoundGeneric);

        if (stage.Matches.Count > 0)
        {
            throw new InvalidOperationException(ErrorMessages.Match.StageAlreadyHasMatches);
        }

        List<Match> matches = stage.StageType switch
        {
            StageType.Group => await BuildGroupStageMatchesAsync(stage),
            StageType.QuarterFinal => await CreateKnockoutStageMatchesAsync(stage),
            StageType.SemiFinal => await CreateKnockoutStageMatchesAsync(stage),
            StageType.ThirdPlace => await CreateFinalStageMatchesAsync(stage),
            StageType.Final => await CreateFinalStageMatchesAsync(stage),
            _ => throw new NotSupportedException(ErrorMessages.Match.StageTypeNotSupportedForAutomatedCreation)
        };


        await matchRepository.AddRangeAsync(matches);
        return matches;
    }

    private static Match BuildMatch(Stage stage, DateTime matchDate, MatchType matchType = MatchType.Playoff)
    {
        return new()
        {
            StageId = stage.Id,
            Type = matchType,
            IsFinished = false,
            MatchDate = matchDate,
            CreatedBy = AuditConstants.SystemUser
        };
    }

    private async Task<int> ResolveGroupTeamCountAsync(Stage stage)
    {
        int totalGroups = await stageRepository.CountAsync(s =>
            s.DivisionId == stage.DivisionId && s.StageType == StageType.Group);

        if (totalGroups <= 0)
        {
            throw new InvalidOperationException(ErrorMessages.Match.NoGroupStagesForDivision);
        }

        int registeredTeams = await teamRepository.CountAsync(team => team.TournamentId == stage.Division.TournamentId);

        if (registeredTeams <= 0)
        {
            throw new InvalidOperationException(ErrorMessages.Match.NoTeamsRegistered);
        }

        if (registeredTeams % totalGroups != 0)
        {
            throw new InvalidOperationException(
                ErrorMessages.Match.TeamsNotDistributableAcrossGroups(registeredTeams, totalGroups));
        }

        int teamsPerGroup = registeredTeams / totalGroups;

        return teamsPerGroup < 2
            ? throw new InvalidOperationException(ErrorMessages.Match.NotEnoughTeamsPerGroup)
            : teamsPerGroup;
    }

    /// <summary>
    /// Resolves the concrete team identities to pair for this group stage,
    /// so the generated matches can actually be seeded (not left empty).
    /// Prefers teams explicitly assigned to this stage via StageTeamMatch;
    /// falls back to the tournament's full roster only when the division
    /// has a single group stage (otherwise there is no way to know which
    /// specific teams belong to this stage). Returns an empty list when
    /// neither source unambiguously yields exactly <paramref name="expectedTeamCount"/>
    /// teams — the matches are still created, just left unseeded, exactly
    /// like before this pairing logic existed.
    /// </summary>
    private async Task<List<Guid>> ResolveGroupTeamIdsAsync(Stage stage, int expectedTeamCount)
    {
        List<Guid> assignedTeamIds = [.. (await stageTeamMatchRepository.FindAsync(stm => stm.StageId == stage.Id))
            .Select(stm => stm.TeamId)];

        if (assignedTeamIds.Count == expectedTeamCount)
        {
            return assignedTeamIds;
        }

        int totalGroups = await stageRepository.CountAsync(s =>
            s.DivisionId == stage.DivisionId && s.StageType == StageType.Group);

        if (totalGroups != 1)
        {
            return [];
        }

        List<Team> registeredTeams = [.. await teamRepository.FindAsync(team => team.TournamentId == stage.Division.TournamentId)];

        return registeredTeams.Count == expectedTeamCount
            ? [.. registeredTeams.Select(t => t.Id)]
            : [];
    }

    private async Task<List<Match>> BuildGroupStageMatchesAsync(Stage stage)
    {
        int teamCount = await ResolveGroupTeamCountAsync(stage);
        List<Guid> teamIds = await ResolveGroupTeamIdsAsync(stage, teamCount);
        return await CreateGroupStageMatchesAsync(stage, teamCount, teamIds);
    }

    /// <summary>
    /// Creates the group stage's matches. When <paramref name="teamIds"/>
    /// unambiguously matches <paramref name="totalTeams"/>, each match is
    /// actually seeded with a home/visitor pair from a freshly randomized
    /// round-robin fixture (repeated per Stage.RoundRobinLegs); otherwise
    /// the matches are created empty, exactly as before this pairing logic
    /// existed, for the admin to assign manually.
    /// </summary>
    private static Task<List<Match>> CreateGroupStageMatchesAsync(Stage stage, int totalTeams, List<Guid> teamIds)
    {
        List<Match> matches = [];

        int totalMatches = totalTeams * (totalTeams - 1) / 2 * stage.RoundRobinLegs;
        List<DateTime> matchDates = DistributeMatchDates(stage.StartDate, stage.EndDate, totalMatches);

        List<(Guid HomeTeamId, Guid VisitorTeamId)> fixture = teamIds.Count == totalTeams
            ? RoundRobinScheduler.GenerateFixture(teamIds, stage.RoundRobinLegs)
            : [];

        for (int i = 0; i < totalMatches; i++)
        {
            Match match = BuildMatch(stage, matchDates[i], MatchType.Regular);

            if (i < fixture.Count)
            {
                match.HomeTeamId = fixture[i].HomeTeamId;
                match.VisitorTeamId = fixture[i].VisitorTeamId;
            }

            matches.Add(match);
        }

        return Task.FromResult(matches);
    }

    private static Task<List<Match>> CreateKnockoutStageMatchesAsync(Stage stage)
    {
        List<Match> matches = [];
        int matchCount = stage.StageType switch
        {
            StageType.QuarterFinal => KnockoutMatchCount.QUARTER_FINAL,
            StageType.SemiFinal => KnockoutMatchCount.SEMI_FINAL,
            _ => throw new InvalidOperationException(ErrorMessages.Match.InvalidKnockoutStageType)
        };

        List<DateTime> matchDates = DistributeMatchDates(stage.StartDate, stage.EndDate, matchCount);

        for (int i = 0; i < matchCount; i++)
        {
            matches.Add(BuildMatch(stage, matchDates[i]));
        }

        return Task.FromResult(matches);
    }

    private static Task<List<Match>> CreateFinalStageMatchesAsync(Stage stage)
    {
        List<Match> matches = [];
        List<DateTime> matchDates = DistributeMatchDates(stage.StartDate, stage.EndDate, 1);

        matches.Add(BuildMatch(stage, matchDates[0]));

        return Task.FromResult(matches);
    }

    private static List<DateTime> DistributeMatchDates(DateTime startDate, DateTime endDate, int matchCount)
    {
        if (matchCount <= 0)
        {
            throw new ArgumentException(ErrorMessages.Match.MatchCountMustBePositive, nameof(matchCount));
        }

        if (endDate < startDate)
        {
            throw new ArgumentException(ErrorMessages.Match.EndDateBeforeStartDate);
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
