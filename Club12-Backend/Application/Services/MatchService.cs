using Application.DTOs.Abstract.Response;
using Application.DTOs.Match.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Constants;
using Application.Utils.Constants.Stage;
using Application.Utils.Extensions;
using Application.Utils.Helper.MatchResult;
using Application.Utils.Helper.Playoff;
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

/// <summary>
/// Manages individual matches: CRUD, result and walkover and suspension transitions, and automated fixture generation.
/// </summary>
public class MatchService(IUnitOfWork unitOfWork, IStageService stageService) : IMatchService
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
    /// Retrieves a match by id or slug with its home and visitor teams, auto-detecting which one was passed.
    /// </summary>
    /// <param name="idOrSlug">The match's GUID id or its slug.</param>
    /// <returns>The match entity if found; otherwise, null.</returns>
    public async Task<Match?> GetMatchByIdOrSlugAsync(string idOrSlug)
    {
        // Loads the full public-detail graph (both teams, venue, and scorers with
        // their players) so the match page can render the scoreboard AND the
        // per-team goleadores — the generic include list can't express the nested
        // Scorers.Player path scorer→team attribution needs.
        return await _matchRepository.GetDetailByIdOrSlugAsync(idOrSlug);
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

    /// <summary>
    /// True if another match is scheduled strictly within a 2-hour window of matchDate at the same venue.
    /// </summary>
    public async Task<bool> HasVenueScheduleConflictAsync(Guid venueId, DateTime matchDate, Guid excludeMatchId)
    {
        DateTime windowStart = matchDate.AddHours(-2);
        DateTime windowEnd = matchDate.AddHours(2);

        return await _matchRepository.ExistsAsync(match =>
            match.Id != excludeMatchId
            && match.VenueId == venueId
            && match.MatchDate > windowStart
            && match.MatchDate < windowEnd);
    }

    public async Task UpdateMatchAsync(Match matchEntity)
    {
        await _matchRepository.UpdateAsync(matchEntity);
    }

    /// <summary>
    /// Loads a decisive final result for a match, rejecting a tied score.
    /// </summary>
    /// <param name="matchId">The id of the match to load.</param>
    /// <param name="homeScore">The home team's final score.</param>
    /// <param name="visitorScore">The visitor team's final score.</param>
    /// <returns>The updated match, or null if no match with that id exists.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the score is tied.</exception>
    public async Task<Match?> LoadMatchResultAsync(Guid matchId, int homeScore, int visitorScore)
    {
        Match? match = await _matchRepository.GetByIdAsync(matchId,
            includes: [m => m.HomeTeam!, m => m.VisitorTeam!, m => m.Stage]);

        if (match is null)
        {
            return null;
        }

        MatchResultFinalizer.ApplyResult(match, homeScore, visitorScore);

        await _matchRepository.UpdateAsync(match);
        await stageService.TryAutoSeedPlayoffPhaseAsync(match.StageId);
        return match;
    }

    /// <summary>
    /// Marks a match as a walkover, awarding the present team the win and the absent team zero.
    /// </summary>
    /// <param name="matchId">The id of the match.</param>
    /// <param name="presentTeamId">The team that showed up, the winner by walkover.</param>
    /// <param name="presentTeamScore">Optional override for the present team's awarded score; defaults to the regulation value.</param>
    /// <returns>The updated match, or null if no match with that id exists.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the present team is not part of the match.</exception>
    public async Task<Match?> LoadWalkOverAsync(Guid matchId, Guid presentTeamId, int? presentTeamScore)
    {
        Match? match = await _matchRepository.GetByIdAsync(matchId,
            includes: [m => m.HomeTeam!, m => m.VisitorTeam!, m => m.Stage]);

        if (match is null)
        {
            return null;
        }

        bool presentIsHome = match.HomeTeamId == presentTeamId;
        bool presentIsVisitor = match.VisitorTeamId == presentTeamId;

        if (!presentIsHome && !presentIsVisitor)
        {
            throw new InvalidOperationException(ErrorMessages.Match.WalkOverTeamNotInMatch);
        }

        int winnerScore = presentTeamScore ?? MatchDefaults.WalkOverWinnerScore;

        match.HomeScore = presentIsHome ? winnerScore : MatchDefaults.WalkOverLoserScore;
        match.VisitorScore = presentIsHome ? MatchDefaults.WalkOverLoserScore : winnerScore;

        match.WinningTeam = presentIsHome ? match.HomeTeam : match.VisitorTeam;
        match.WinningTeamId = presentTeamId;

        match.IsFinished = true;
        match.Status = MatchStatus.WalkOver;

        await _matchRepository.UpdateAsync(match);
        await stageService.TryAutoSeedPlayoffPhaseAsync(match.StageId);
        return match;
    }

    /// <summary>
    /// Reprograms or suspends a match, moving it to a new date without touching its Round.
    /// </summary>
    /// <param name="matchId">The id of the match to suspend/reprogram.</param>
    /// <param name="newMatchDate">Optional new calendar date; when null the existing date is kept.</param>
    /// <returns>The updated match, or null if no match with that id exists.</returns>
    public async Task<Match?> SuspendMatchAsync(Guid matchId, DateTime? newMatchDate)
    {
        Match? match = await _matchRepository.GetByIdAsync(matchId,
            includes: [m => m.HomeTeam!, m => m.VisitorTeam!]);

        if (match is null)
        {
            return null;
        }

        match.Status = MatchStatus.Suspended;

        if (newMatchDate.HasValue)
        {
            match.MatchDate = newMatchDate.Value;
        }

        await _matchRepository.UpdateAsync(match);
        return match;
    }

    public async Task<List<Match>> GetStageMatchesByRoundAsync(Guid stageId)
    {
        IEnumerable<Match> matches = await _matchRepository.FindAsync(
            match => match.StageId == stageId,
            includes: [match => match.HomeTeam!, match => match.VisitorTeam!, match => match.Venue!]);

        // Round is the canonical grouping: order by matchday, then by
        // date within the round for a stable "Partido 1, Partido 2, …" order.
        // Matches without a round sort after the numbered ones.
        return [.. matches
            .OrderBy(match => match.Round ?? int.MaxValue)
            .ThenBy(match => match.MatchDate)];
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

    /// <summary>
    /// Builds a stage's fixture from its type, blocked once the stage already has matches.
    /// </summary>
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
    /// Resolves a team's name for slug composition without requiring the caller to have loaded Team.
    /// </summary>
    /// <param name="teamId">The team's id, or null when no team is assigned.</param>
    /// <returns>The team's name, or MatchSlugSourceBuilder.UnassignedTeamPlaceholder
    /// when teamId is null or does not resolve to a team.</returns>
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
    /// Assigns a unique slug to every match in a freshly built batch before it is persisted.
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

        Dictionary<Match, string> baseSlugByMatch = matches.ToDictionary(
            match => match,
            match => SlugGenerator.GenerateSlug(MatchSlugSourceBuilder.Build(
                ResolveTeamNameFromMap(match.HomeTeamId, teamNamesById),
                ResolveTeamNameFromMap(match.VisitorTeamId, teamNamesById),
                match.MatchDate)));

        HashSet<string> baseSlugs = [.. baseSlugByMatch.Values.Distinct()];
        HashSet<string> existingSlugs = [.. (await _matchRepository.FindAsync(
            m => baseSlugs.Contains(m.Slug)))
            .Select(m => m.Slug)];

        HashSet<string> slugsAssignedInBatch = [];

        foreach (Match match in matches)
        {
            string baseSlug = baseSlugByMatch[match];

            // Fast, IO-free path: the prefetch already proved this exact
            // base slug is free, and nothing earlier in this batch claimed
            // it either.
            if (!existingSlugs.Contains(baseSlug) && !slugsAssignedInBatch.Contains(baseSlug))
            {
                match.Slug = baseSlug;
            }
            else
            {
                string homeTeamName = ResolveTeamNameFromMap(match.HomeTeamId, teamNamesById);
                string visitorTeamName = ResolveTeamNameFromMap(match.VisitorTeamId, teamNamesById);

                match.Slug = await SlugGenerator.GenerateUniqueSlugAsync(
                    MatchSlugSourceBuilder.Build(homeTeamName, visitorTeamName, match.MatchDate),
                    async candidate => slugsAssignedInBatch.Contains(candidate)
                        || existingSlugs.Contains(candidate)
                        || await _matchRepository.ExistsAsync(m => m.Slug == candidate));
            }

            slugsAssignedInBatch.Add(match.Slug);
        }
    }

    private static string ResolveTeamNameFromMap(Guid? teamId, IReadOnlyDictionary<Guid, string> teamNamesById)
    {
        return teamId.HasValue && teamNamesById.TryGetValue(teamId.Value, out string? name)
            ? name
            : MatchSlugSourceBuilder.UnassignedTeamPlaceholder;
    }

    /// <summary>
    /// Builds an unpersisted match for a stage's automated fixture with a placeholder slug.
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
    /// Resolves team identities to pair for a group stage with no explicit StageTeamMatch assignments yet.
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
    /// A stage with explicit StageTeamMatch assignments always uses exactly those teams for pairing.
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
    /// Creates the group stage's matches from a round-robin fixture organised by matchday.
    /// </summary>
    private static Task<List<Match>> CreateGroupStageMatchesAsync(Stage stage, int totalTeams, List<Guid> teamIds)
    {
        List<Match> matches = [];

        bool seeded = teamIds.Count == totalTeams;

        // When the roster is unknown we still need the round structure (how
        // many matches, and each one's matchday), which depends only on the
        // team count and legs — so schedule with throwaway placeholder ids and
        // keep just the round numbers, leaving the teams unseeded.
        IReadOnlyList<Guid> rosterForSchedule = seeded
            ? teamIds
            : [.. Enumerable.Range(0, totalTeams).Select(_ => Guid.NewGuid())];

        List<(Guid HomeTeamId, Guid VisitorTeamId, int Round)> fixture =
            RoundRobinScheduler.GenerateRounds(rosterForSchedule, stage.RoundRobinLegs);

        // Lay the jornadas out weekly, division-aware. Regular zones
        // keep the Sunday baseline; a cross-division-cup stage is shifted to a
        // different weekday so a team playing both its zone and the cup never
        // has two jornadas on the same day. Coordinated here (not at the
        // tournament level) so BOTH the manual single-stage generate endpoint
        // and the tournament-start fixture trigger (GenerateFixtureAsync, which
        // calls this same path) get the anti-collision schedule.
        bool isCrossDivisionCup = stage.Division?.IsCrossDivisionCup ?? false;

        foreach ((Guid homeTeamId, Guid visitorTeamId, int round) in fixture)
        {
            Match match = BuildMatch(stage, RoundCalendar.DateForRound(stage.StartDate, round, isCrossDivisionCup), MatchType.Regular);
            match.Round = round;

            if (seeded)
            {
                match.HomeTeamId = homeTeamId;
                match.VisitorTeamId = visitorTeamId;
            }

            matches.Add(match);
        }

        return Task.FromResult(matches);
    }

    private async Task<List<Match>> CreateKnockoutStageMatchesAsync(Stage stage)
    {
        List<Match> matches = [];
        int matchCount = await ResolveKnockoutFirstRoundMatchCountAsync(stage);

        List<DateTime> matchDates = DistributeMatchDates(stage.StartDate, stage.EndDate, matchCount);

        for (int i = 0; i < matchCount; i++)
        {
            matches.Add(BuildMatch(stage, matchDates[i]));
        }

        return matches;
    }

    /// <summary>
    /// Number of empty first-round matches to create for a bracket stage.
    /// </summary>
    private async Task<int> ResolveKnockoutFirstRoundMatchCountAsync(Stage stage)
    {
        if (stage.Division.IsCrossDivisionCup)
        {
            List<Stage> groupStages = [.. await _stageRepository.FindAsync(
                s => s.DivisionId == stage.DivisionId && s.StageType == StageType.Group)];

            if (groupStages.Count > 1)
            {
                int totalQualifiers = 0;
                foreach (Stage groupStage in groupStages)
                {
                    int groupSize = await _stageTeamMatchRepository.CountAsync(stm => stm.StageId == groupStage.Id);
                    totalQualifiers += Math.Min(stage.Division.QualifiersPerGroup, groupSize);
                }

                return PlayoffSeeder.NextPowerOfTwo(totalQualifiers) / 2;
            }
        }

        return stage.StageType switch
        {
            StageType.QuarterFinal => KnockoutMatchCount.QUARTER_FINAL,
            StageType.SemiFinal => KnockoutMatchCount.SEMI_FINAL,
            _ => throw new InvalidOperationException(ErrorMessages.Match.InvalidKnockoutStageType)
        };
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
