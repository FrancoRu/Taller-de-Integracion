using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces.Services;
using Domain.Entities.Models;
using Domain.Enums;
using Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Characterization (approval) tests for <see cref="Application.Services.MatchService"/>'s
/// double round-robin fixture generation (<c>GenerateFixtureAsync</c>) and group-stage match
/// creation (the <c>Group</c> path of <c>CreateAutomatedMatchesAsync</c>), driven black-box
/// through <see cref="IMatchService"/> via the real host + in-memory SQLite, mirroring
/// <see cref="AutomatedMatchGenerationTests"/>.
///
/// Two behaviors are deliberately characterized as-is, NOT fixed here (see proposal/design):
/// BUG-1 — generated fixture matches never get a <c>StageId</c> and ignore the
/// <c>divisionId</c> argument. BUG-2 — every match within one fixture leg (first or second)
/// shares one identical <c>MatchDate</c>; only the two legs differ.
/// </summary>
public class MatchServiceGenerationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MatchServiceGenerationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---------------------------------------------------------------------
    // Phase 2: GenerateFixtureAsync — Double Round-Robin Fixture Generation
    // ---------------------------------------------------------------------

    // DISCOVERY (deviation from design.md's assumption — see apply report): design.md assumed
    // GenerateFixtureAsync's matches persist successfully with StageId left at Guid.Empty.
    // Empirically, that assumption is wrong: Match.StageId is a REQUIRED foreign key to Stage
    // (see MatchEntityConfiguration — `builder.HasOne(m => m.Stage)...IsRequired()`), and no
    // Stage row with Id == Guid.Empty exists in any real database. Because BUG-1 never assigns
    // StageId, persisting the generated matches ALWAYS fails with a foreign key violation
    // (wrapped by EF Core as DbUpdateException) — against this FK-enforcing SQLite harness and
    // equally against the real Postgres schema, which defines the same required relationship.
    // This is characterized below instead of the originally planned "assert StageId ==
    // Guid.Empty on persisted rows" scenario, which is unobservable: the insert never succeeds.
    // As a direct consequence, the round-robin rotation/pairing/home-away-swap requirements and
    // BUG-2 (identical MatchDate per leg) are also unobservable via this black-box, no-
    // InternalsVisibleTo integration surface for ANY valid team count — there is no successful
    // persisted state to read back. Source-level confirmation of BUG-2 (currentMatchDate
    // captured once per leg and reused unmodified for every match in that leg) is documented
    // below rather than pinned by a runnable assertion, mirroring the treatment already used for
    // the two other unreachable guards in this file (totalGroups<=0, matchCount<=0).

    /// <summary>
    /// BUG-1 (characterized, not fixed): for every valid (even, &gt;=2) team count,
    /// <c>GenerateFixtureAsync</c> throws <see cref="DbUpdateException"/> when persisting,
    /// because the generated matches keep the CLR default <see cref="Guid.Empty"/> for the
    /// required <c>StageId</c> foreign key. No matches are persisted.
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    public async Task GenerateFixtureAsync_ValidTeamCount_ThrowsForeignKeyViolation_Bug1(int teamCount)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        List<Team> teams = await SeedTeamsAsync(db, teamCount);
        List<Guid> teamIds = [.. teams.Select(t => t.Id)];

        await Assert.ThrowsAsync<DbUpdateException>(
            () => matchService.GenerateFixtureAsync(Guid.NewGuid(), teams));

        List<Match> matches = await QueryGeneratedMatchesAsync(db, teamIds);
        Assert.Empty(matches);
    }

    /// <summary>
    /// BUG-1 (characterized, not fixed): the <c>divisionId</c> parameter is never read by
    /// <c>GenerateFixtureAsync</c> — the identical foreign key violation occurs regardless of
    /// which <c>divisionId</c> is supplied, confirming the argument has no effect on behavior.
    /// </summary>
    [Fact]
    public async Task GenerateFixtureAsync_DivisionIdArgumentDoesNotAffectFailure_Bug1()
    {
        // Two separate scopes (and DbContext instances) — mirroring the real per-request
        // DbContext lifetime — because a failed SaveChangesAsync leaves the offending Match
        // entities tracked as Added in that DbContext's change tracker, which would otherwise
        // make a second call on the SAME context fail for an unrelated reason (re-attempting to
        // insert the still-tracked broken entities from the first call).
        using IServiceScope scopeA = _factory.Services.CreateScope();
        ApplicationDBContext dbA = scopeA.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchServiceA = scopeA.ServiceProvider.GetRequiredService<IMatchService>();

        List<Team> teamsA = await SeedTeamsAsync(dbA, 4);
        DbUpdateException exceptionA = await Assert.ThrowsAsync<DbUpdateException>(
            () => matchServiceA.GenerateFixtureAsync(Guid.NewGuid(), teamsA));

        using IServiceScope scopeB = _factory.Services.CreateScope();
        ApplicationDBContext dbB = scopeB.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchServiceB = scopeB.ServiceProvider.GetRequiredService<IMatchService>();

        List<Team> teamsB = await SeedTeamsAsync(dbB, 4);
        DbUpdateException exceptionB = await Assert.ThrowsAsync<DbUpdateException>(
            () => matchServiceB.GenerateFixtureAsync(Guid.NewGuid(), teamsB));

        Assert.Equal(exceptionA.GetType(), exceptionB.GetType());
    }

    [Theory]
    [InlineData(5)]
    [InlineData(0)]
    [InlineData(1)]
    public async Task GenerateFixtureAsync_InvalidTeamCount_ThrowsAndPersistsNoMatches(int teamCount)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        List<Team> teams = await SeedTeamsAsync(db, teamCount);
        List<Guid> teamIds = [.. teams.Select(t => t.Id)];

        await Assert.ThrowsAsync<ArgumentException>(
            () => matchService.GenerateFixtureAsync(Guid.NewGuid(), teams));

        List<Match> matches = await QueryGeneratedMatchesAsync(db, teamIds);
        Assert.Empty(matches);
    }

    // ---------------------------------------------------------------------
    // Phase 3: CreateAutomatedMatchesAsync (Group path) — team count resolution
    // ---------------------------------------------------------------------

    // NOTE (documented, not independently testable): ResolveGroupTeamCountAsync's
    // "totalGroups <= 0" guard is unreachable through the public surface. The stage passed to
    // CreateAutomatedMatchesAsync must itself be a persisted Group-type stage to reach this code
    // path at all, and the guard's own count query (`DivisionId == stage.DivisionId &&
    // StageType == Group`) always includes that same stage — so totalGroups is guaranteed >= 1.
    // This mirrors the DistributeMatchDates matchCount<=0 guard noted below.

    [Fact]
    public async Task CreateAutomatedMatchesAsync_GroupStage_NoTeamsRegistered_ThrowsInvalidOperationException()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        (Tournament tournament, Division division) = await SeedDivisionAsync(db);
        List<Stage> stages = await SeedGroupStagesAsync(db, division, groupCount: 1);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => matchService.CreateAutomatedMatchesAsync(stages[0].Id));
    }

    [Fact]
    public async Task CreateAutomatedMatchesAsync_GroupStage_TeamsNotDivisibleByGroupCount_Throws()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        (Tournament tournament, Division division) = await SeedDivisionAsync(db);
        await SeedTeamsAsync(db, 10, tournament.Id);
        List<Stage> stages = await SeedGroupStagesAsync(db, division, groupCount: 3);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => matchService.CreateAutomatedMatchesAsync(stages[0].Id));
    }

    [Fact]
    public async Task CreateAutomatedMatchesAsync_GroupStage_FewerThanTwoTeamsPerGroup_Throws()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        (Tournament tournament, Division division) = await SeedDivisionAsync(db);
        await SeedTeamsAsync(db, 2, tournament.Id);
        List<Stage> stages = await SeedGroupStagesAsync(db, division, groupCount: 2);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => matchService.CreateAutomatedMatchesAsync(stages[0].Id));
    }

    [Theory]
    [InlineData(4, 2, 6)]  // 8 teams / 2 groups => 4 teams/group => 4*3/2 = 6 matches
    [InlineData(8, 2, 28)] // 16 teams / 2 groups => 8 teams/group => 8*7/2 = 28 matches
    public async Task CreateAutomatedMatchesAsync_GroupStage_ValidDistribution_CreatesRoundRobinMatches(
        int teamsPerGroup, int groupCount, int expectedMatchCount)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        (_, _, List<Stage> stages, _) = await SeedGroupStageWithTeamsAsync(db, teamsPerGroup, groupCount);

        List<Match> matches = await matchService.CreateAutomatedMatchesAsync(stages[0].Id);

        Assert.Equal(expectedMatchCount, matches.Count);
        Assert.All(matches, m => Assert.Equal(stages[0].Id, m.StageId));
        Assert.All(matches, m => Assert.Equal(MatchType.Regular, m.Type));
    }

    // ---------------------------------------------------------------------
    // Phase 4: Match date distribution, reachable via the group path only
    // ---------------------------------------------------------------------

    [Fact]
    public async Task CreateAutomatedMatchesAsync_GroupStage_SingleMatch_UsesRangeMidpoint()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        DateTime start = DateTime.UtcNow.Date;
        DateTime end = start.AddDays(10);

        // teamsPerGroup=2 => totalMatches = 2*1/2 = 1
        (_, _, List<Stage> stages, _) = await SeedGroupStageWithTeamsAsync(db, teamsPerGroup: 2, groupCount: 1, start, end);

        List<Match> matches = await matchService.CreateAutomatedMatchesAsync(stages[0].Id);

        Assert.Single(matches);
        DateTime expectedDate = start.AddDays((end - start).TotalDays / 2);
        Assert.Equal(expectedDate, matches[0].MatchDate);
    }

    [Fact]
    public async Task CreateAutomatedMatchesAsync_GroupStage_MultipleMatches_SpreadEvenlyAcrossRange()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        DateTime start = DateTime.UtcNow.Date;
        DateTime end = start.AddDays(10);

        // teamsPerGroup=3 => totalMatches = 3*2/2 = 3
        (_, _, List<Stage> stages, _) = await SeedGroupStageWithTeamsAsync(db, teamsPerGroup: 3, groupCount: 1, start, end);

        List<Match> matches = await matchService.CreateAutomatedMatchesAsync(stages[0].Id);
        List<Match> ordered = [.. matches.OrderBy(m => m.MatchDate)];

        Assert.Equal(3, ordered.Count);
        Assert.Equal(start, ordered[0].MatchDate);
        Assert.Equal(end, ordered[^1].MatchDate);

        double totalDays = (end - start).TotalDays;
        double interval = totalDays / (ordered.Count - 1);
        for (int i = 0; i < ordered.Count; i++)
        {
            DateTime expected = start.AddDays(interval * i);
            Assert.Equal(expected, ordered[i].MatchDate);
        }
    }

    [Fact]
    public async Task CreateAutomatedMatchesAsync_GroupStage_EndDateBeforeStartDate_ThrowsArgumentException()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        DateTime start = DateTime.UtcNow.Date;
        DateTime end = start.AddDays(-1);

        (_, _, List<Stage> stages, _) = await SeedGroupStageWithTeamsAsync(db, teamsPerGroup: 2, groupCount: 1, start, end);

        await Assert.ThrowsAsync<ArgumentException>(
            () => matchService.CreateAutomatedMatchesAsync(stages[0].Id));
    }

    // NOTE (documented, not independently testable): DistributeMatchDates' "matchCount <= 0"
    // guard is unreachable through the public surface — every public caller (group/knockout/
    // final match creation) always passes a computed, strictly positive match count.

    // ---------------------------------------------------------------------
    // Seed helpers (local to this file, no shared/production code changes)
    // ---------------------------------------------------------------------

    private static async Task<List<Team>> SeedTeamsAsync(ApplicationDBContext db, int count, Guid? tournamentId = null)
    {
        List<Team> teams = [];
        for (int i = 0; i < count; i++)
        {
            teams.Add(new Team
            {
                Name = $"Team-{i}-{Guid.NewGuid()}",
                ThreeLetterCode = $"T{i:D2}",
                LogoUrl = "https://example.com/logo.png",
                ShirtColor = "Red",
                TournamentId = tournamentId,
                Players = [],
                CreatedBy = "test",
            });
        }

        if (teams.Count > 0)
        {
            db.Teams.AddRange(teams);
            await db.SaveChangesAsync();
        }

        return teams;
    }

    private static async Task<(Tournament tournament, Division division)> SeedDivisionAsync(ApplicationDBContext db)
    {
        DateTime start = DateTime.UtcNow.Date;

        Tournament tournament = new()
        {
            Description = "Characterization test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = start.AddDays(-1),
            StartDate = start,
            MaxTeams = 64,
            MinTeams = 2,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        Division division = new()
        {
            Name = $"Division-{Guid.NewGuid()}",
            Tournament = tournament,
            Stages = [],
            CreatedBy = "test",
        };

        db.Tournaments.Add(tournament);
        db.Divisions.Add(division);
        await db.SaveChangesAsync();

        return (tournament, division);
    }

    private static async Task<List<Stage>> SeedGroupStagesAsync(
        ApplicationDBContext db, Division division, int groupCount, DateTime? startDate = null, DateTime? endDate = null)
    {
        DateTime start = startDate ?? DateTime.UtcNow.Date;
        DateTime end = endDate ?? start.AddDays(14);

        List<Stage> stages = [];
        for (int i = 0; i < groupCount; i++)
        {
            stages.Add(new Stage
            {
                Name = $"Group-{i}-{Guid.NewGuid()}",
                StageType = StageType.Group,
                IsActive = true,
                StartDate = start,
                EndDate = end,
                DivisionId = division.Id,
                Division = division,
                Matches = [],
                CreatedBy = "test",
            });
        }

        db.Stages.AddRange(stages);
        await db.SaveChangesAsync();

        return stages;
    }

    private static async Task<(Tournament tournament, Division division, List<Stage> stages, List<Team> teams)>
        SeedGroupStageWithTeamsAsync(
            ApplicationDBContext db, int teamsPerGroup, int groupCount,
            DateTime? startDate = null, DateTime? endDate = null)
    {
        (Tournament tournament, Division division) = await SeedDivisionAsync(db);
        List<Team> teams = await SeedTeamsAsync(db, teamsPerGroup * groupCount, tournament.Id);
        List<Stage> stages = await SeedGroupStagesAsync(db, division, groupCount, startDate, endDate);

        return (tournament, division, stages, teams);
    }

    private static async Task<List<Match>> QueryGeneratedMatchesAsync(ApplicationDBContext db, List<Guid> teamIds)
        => await db.Matches
            .Where(m => m.HomeTeamId.HasValue && teamIds.Contains(m.HomeTeamId.Value))
            .ToListAsync();
}
