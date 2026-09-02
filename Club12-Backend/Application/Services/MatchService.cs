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
    /// Retrieves a match by its id or its slug, including its home/visitor
    /// teams. The value is treated as an id when it parses as a GUID,
    /// otherwise it is looked up as a slug.
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

    public async Task<bool> HasVenueScheduleConflictAsync(Guid venueId, DateTime matchDate, Guid excludeMatchId)
    {
        // Two matches on the same court must be at least 2 hours apart, so a
        // conflict is any OTHER match at this venue strictly within the ±2h
        // window (exactly 2 hours apart is allowed).
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
    /// Loads a decisive final result for a match (HU-69/HU-70). Basketball has
    /// no draws, so an equal score is rejected with a stage-appropriate message
    /// (group stage vs. playoff overtime) instead of silently picking a winner.
    /// On success the match becomes <see cref="MatchStatus.Played"/>, IsFinished
    /// is set, and the winning team is derived from the higher score.
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
    /// Marks a match as a walkover (HU-73): the present team is awarded the
    /// regulation default result (<see cref="MatchDefaults.WalkOverWinnerScore"/>-0,
    /// or a caller-provided winner score) and the absent team gets zero. The
    /// match becomes <see cref="MatchStatus.WalkOver"/> so it stays
    /// distinguishable from a normally played result while still counting in
    /// standings and statistics like any finished, decisive match.
    /// </summary>
    /// <param name="matchId">The id of the match.</param>
    /// <param name="presentTeamId">The team that showed up (the winner by walkover).</param>
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
    /// Reprograms/suspends a match (HU-68). The match becomes
    /// <see cref="MatchStatus.Suspended"/> and, when a new date is supplied,
    /// moves to it. Its <see cref="Match.Round"/> is deliberately left
    /// untouched (HU-67): suspending or rescheduling never changes the matchday
    /// a game belongs to, nor does it affect any other match in the fixture.
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

        // Round is the canonical grouping (HU-63): order by matchday, then by
        // date within the round for a stable "Partido 1, Partido 2, …" order.
        // Matches without a round (e.g. knockout) sort after the numbered ones.
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
    /// prefetched once for the whole batch to avoid N+1 queries, and so is
    /// slug uniqueness: every match's base slug is pure/synchronous, so the
    /// whole batch's candidates are checked against already-persisted
    /// matches in ONE query up front, instead of one EXISTS round trip per
    /// match. A real collision (rare — slugs are team names plus a
    /// timestamp) still falls back to a live per-candidate check via
    /// GenerateUniqueSlugAsync's normal -2/-3 retry loop, since the
    /// suffixed candidates aren't covered by the prefetch.
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
    /// Creates the group stage's matches from a round-robin fixture organised
    /// by matchday (jornada, HU-63/HU-65). The number of rounds is derived from
    /// the team count and <see cref="Stage.RoundRobinLegs"/>; every match is
    /// tagged with its 1-based <see cref="Match.Round"/> and given a default,
    /// division-aware weekly date for that round (HU-65/HU-111): regular zones
    /// on Sundays, a cross-division cup shifted to a different weekday so its
    /// jornadas never collide with the zones a shared team also plays in. Round
    /// numbers — not the calendar date — are the canonical fixture grouping.
    /// <para>
    /// When <paramref name="teamIds"/> unambiguously matches
    /// <paramref name="totalTeams"/>, each match is seeded with a home/visitor
    /// pair; otherwise the matches are created unseeded (teams left null) for
    /// the admin to assign manually, but still keep their round structure so a
    /// later assignment slots them into the right matchday.
    /// </para>
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

        // HU-111: lay the jornadas out weekly, division-aware. Regular zones
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
    /// <para>
    /// A multi-group cross-division cup (HU-110) is sized from its pooled
    /// qualifiers instead of the fixed per-stage-type count: with
    /// <c>totalQualifiers = Σ min(QualifiersPerGroup, groupSize)</c> across
    /// every internal group, the first round needs
    /// <c>NextPowerOfTwo(totalQualifiers) / 2</c> matches so
    /// <see cref="Application.Utils.Helper.Playoff.PlayoffSeeder.SeedPairs"/>
    /// (which pads the seed pool up to the next power of two with byes) has an
    /// empty match for every pair. A cross cup with a single group, and every
    /// regular division, keeps the original fixed count for its stage type.
    /// </para>
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
