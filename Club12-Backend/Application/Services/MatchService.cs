using Application.DTOs.Abstract.Response;
using Application.DTOs.Match.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Constants;
using Application.Utils.Constants.Stage;
using Application.Utils.Extensions;
using Application.Utils.Helper.RoundRobin;
using Application.Utils.Helper.Slug;

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
    private readonly IMatchRepository _matchRepository = unitOfWork.MatchRepository;
    private readonly IStageRepository _stageRepository = unitOfWork.StageRepository;
    private readonly ITeamRepository _teamRepository = unitOfWork.TeamRepository;
    private readonly IStageTeamMatchRepository _stageTeamMatchRepository = unitOfWork.StageTeamMatchRepository;

    public async Task<Match> CreateMatchAsync(Match matchEntity)
    {
        string homeTeamName = await ResolveTeamNameAsync(matchEntity.HomeTeamId);
        string visitorTeamName = await ResolveTeamNameAsync(matchEntity.VisitorTeamId);

        matchEntity.Slug = await SlugGenerator.GenerateUniqueSlugAsync(
            MatchSlugSourceBuilder.Build(homeTeamName, visitorTeamName, matchEntity.MatchDate),
            candidate => _matchRepository.ExistsAsync(match => match.Slug == candidate));

        await _matchRepository.AddAsync(matchEntity);
        return matchEntity;
    }

    public async Task<Match?> GetMatchByIdAsync(Guid matchId)
    {
        return await _matchRepository.GetByIdAsync(matchId, includes: [m => m.HomeTeam!, m => m.VisitorTeam!]);
    }

    /// <summary>
    /// Retrieves a match by its id or its slug, including its home/visitor
    /// teams. The value is treated as an id when it parses as a GUID,
    /// otherwise it is looked up as a slug.
    /// </summary>
    /// <param name="idOrSlug">The match's GUID id or its slug.</param>
    /// <returns>The match entity if found; otherwise, null.</returns>
    public async Task<Match?> GetMatchByIdOrSlugAsync(string idOrSlug)
    {
        if (Guid.TryParse(idOrSlug, out Guid matchId))
        {
            return await GetMatchByIdAsync(matchId);
        }

        IEnumerable<Match> matches = await _matchRepository.FindAsync(
            match => match.Slug == idOrSlug,
            includes: [match => match.HomeTeam!, match => match.VisitorTeam!]);

        return matches.FirstOrDefault();
    }

    public async Task<Match?> GetMatchByIdWithScorersAsync(Guid matchId)
    {
        Match? match = await _matchRepository.GetByIdAsync(matchId,
            includes: [m => m.HomeTeam!,
                        m => m.VisitorTeam!,
                        m => m.PlayerStatistics,
                        m => m.Venue!]
            );

        return match is null ? null : match;
    }

    public async Task DeleteMatchAsync(Guid id)
    {
        await _matchRepository.RemoveAsync(match => match.Id == id);
    }

    public async Task UpdateMatchAsync(Match matchEntity)
    {
        await _matchRepository.UpdateAsync(matchEntity);
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

        IEnumerable<Match> filteredMatches = await _matchRepository.FindAsync(expression, filter: filter, includes: [match => match.HomeTeam!,
                                                                                                                      match => match.VisitorTeam!,
                                                                                                                      match => match.Venue!]);

        int totalCount = await _matchRepository.CountAsync(expression);

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
        Stage stage = await _stageRepository.GetByIdAsync(stageId, includes: [s => s.Matches, s => s.Division])
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

        await AssignMatchSlugsAsync(matches);

        await _matchRepository.AddRangeAsync(matches);
        return matches;
    }

    /// <summary>
    /// Resolves a team's name for slug composition, without requiring the
    /// caller to have loaded the Team navigation property.
    /// </summary>
    /// <param name="teamId">The team's id, or null when no team is assigned.</param>
    /// <returns>The team's name, or <see cref="MatchSlugSourceBuilder.UnassignedTeamPlaceholder"/>
    /// when <paramref name="teamId"/> is null or does not resolve to a team.</returns>
    private async Task<string> ResolveTeamNameAsync(Guid? teamId)
    {
        if (!teamId.HasValue)
        {
            return MatchSlugSourceBuilder.UnassignedTeamPlaceholder;
        }

        Team? team = await _teamRepository.GetByIdAsync(teamId.Value);
        return team?.Name ?? MatchSlugSourceBuilder.UnassignedTeamPlaceholder;
    }

    /// <summary>
    /// Assigns a unique slug to every match in a freshly built batch (e.g. a
    /// stage's full fixture) before it is persisted. Team names are
    /// prefetched once for the whole batch to avoid N+1 queries. Uniqueness
    /// is checked against both already-persisted matches and the slugs
    /// already assigned earlier in this same batch, since none of the
    /// batch's matches exist in the repository yet when this runs.
    /// </summary>
    private async Task AssignMatchSlugsAsync(List<Match> matches)
    {
        List<Guid> teamIds = [.. matches
            .SelectMany(match => new[] { match.HomeTeamId, match.VisitorTeamId })
            .Where(teamId => teamId.HasValue)
            .Select(teamId => teamId!.Value)
            .Distinct()];

        Dictionary<Guid, string> teamNamesById = teamIds.Count == 0
            ? []
            : (await _teamRepository.FindAsync(team => teamIds.Contains(team.Id)))
                .ToDictionary(team => team.Id, team => team.Name);

        HashSet<string> slugsAssignedInBatch = [];

        foreach (Match match in matches)
        {
            string homeTeamName = ResolveTeamNameFromMap(match.HomeTeamId, teamNamesById);
            string visitorTeamName = ResolveTeamNameFromMap(match.VisitorTeamId, teamNamesById);

            match.Slug = await SlugGenerator.GenerateUniqueSlugAsync(
                MatchSlugSourceBuilder.Build(homeTeamName, visitorTeamName, match.MatchDate),
                async candidate => slugsAssignedInBatch.Contains(candidate)
                    || await _matchRepository.ExistsAsync(m => m.Slug == candidate));

            slugsAssignedInBatch.Add(match.Slug);
        }
    }

    private static string ResolveTeamNameFromMap(Guid? teamId, IReadOnlyDictionary<Guid, string> teamNamesById) =>
        teamId.HasValue && teamNamesById.TryGetValue(teamId.Value, out string? name)
            ? name
            : MatchSlugSourceBuilder.UnassignedTeamPlaceholder;

    /// <summary>
    /// Builds an unpersisted match for a stage's automated fixture. Slug is
    /// a placeholder here — every match built this way is later persisted
    /// only via CreateAutomatedMatchesAsync, which overwrites it with a real
    /// one in AssignMatchSlugsAsync before the batch is saved.
    /// </summary>
    private static Match BuildMatch(Stage stage, DateTime matchDate, MatchType matchType = MatchType.Playoff)
    {
        return new()
        {
            StageId = stage.Id,
            Type = matchType,
            Slug = string.Empty,
            IsFinished = false,
            MatchDate = matchDate,
            CreatedBy = AuditConstants.SystemUser
        };
    }

    private async Task<int> ResolveGroupTeamCountAsync(Stage stage)
    {
        int totalGroups = await _stageRepository.CountAsync(s =>
            s.DivisionId == stage.DivisionId && s.StageType == StageType.Group);

        if (totalGroups <= 0)
        {
            throw new InvalidOperationException(ErrorMessages.Match.NoGroupStagesForDivision);
        }

        int registeredTeams = await _teamRepository.CountAsync(team => team.TournamentId == stage.Division.TournamentId);

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
    /// Resolves the concrete team identities to pair for a group stage that
    /// has NOT had teams explicitly assigned via StageTeamMatch yet (see
    /// <see cref="BuildGroupStageMatchesAsync"/>, which only calls this as a
    /// fallback). Guesses the roster from the tournament's full team list,
    /// but only when the division has a single group stage — otherwise
    /// there is no way to know which specific teams belong to this one.
    /// Returns an empty list when that guess doesn't unambiguously yield
    /// exactly <paramref name="expectedTeamCount"/> teams — the matches are
    /// still created, just left unseeded, exactly like before this pairing
    /// logic existed.
    /// </summary>
    private async Task<List<Guid>> ResolveGroupTeamIdsAsync(Stage stage, int expectedTeamCount)
    {
        int totalGroups = await _stageRepository.CountAsync(s =>
            s.DivisionId == stage.DivisionId && s.StageType == StageType.Group);

        if (totalGroups != 1)
        {
            return [];
        }

        List<Team> registeredTeams = [.. await _teamRepository.FindAsync(team => team.TournamentId == stage.Division.TournamentId)];

        return registeredTeams.Count == expectedTeamCount
            ? [.. registeredTeams.Select(t => t.Id)]
            : [];
    }

    /// <summary>
    /// A stage that already has teams explicitly assigned via StageTeamMatch
    /// (the tournament wizard's flow: register every team to the tournament,
    /// then assign each zone's own subset to its own group stage) always
    /// uses exactly those teams — never the tournament-wide/single-group
    /// guess in <see cref="ResolveGroupTeamCountAsync"/>/<see
    /// cref="ResolveGroupTeamIdsAsync"/>, which assumes the whole
    /// tournament is one division. Without this short-circuit, a
    /// multi-division tournament (any real one with more than one
    /// zone/group) would silently pair every zone's matches from the
    /// tournament's ENTIRE team list instead of that zone's own assigned
    /// teams, because "registered teams ÷ groups in this division" (=
    /// "registered teams ÷ 1") always equals the tournament-wide total, not
    /// this stage's actual roster.
    /// </summary>
    private async Task<List<Match>> BuildGroupStageMatchesAsync(Stage stage)
    {
        List<Guid> assignedTeamIds = [.. (await _stageTeamMatchRepository.FindAsync(stm => stm.StageId == stage.Id))
            .Select(stm => stm.TeamId)];

        if (assignedTeamIds.Count >= 2)
        {
            return await CreateGroupStageMatchesAsync(stage, assignedTeamIds.Count, assignedTeamIds);
        }

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
