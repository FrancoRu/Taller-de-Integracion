using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.Extensions.DependencyInjection;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Characterization tests for group-stage match creation in MatchService.
/// </summary>
public class MatchServiceGenerationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MatchServiceGenerationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateAutomatedMatchesAsync_GroupStage_NoTeamsRegistered_ThrowsInvalidOperationException()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        (_, Division division) = await SeedDivisionAsync(db);
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

    /// <summary>
    /// Expected match counts follow the round-robin formula teamsPerGroup *
    /// (teamsPerGroup - 1) / 2: 8 teams / 2 groups => 4 teams/group => 6
    /// matches; 16 teams / 2 groups => 8 teams/group => 28 matches.
    /// </summary>
    [Theory]
    [InlineData(4, 2, 6)]
    [InlineData(8, 2, 28)]
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

    /// <summary>
    /// Match date distribution is reachable via the group stage path only.
    /// teamsPerGroup=2 produces a single round-robin match (2*1/2 = 1).
    /// </summary>
    [Fact]
    public async Task CreateAutomatedMatchesAsync_GroupStage_SingleMatch_UsesRangeMidpoint()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        DateTime start = DateTime.UtcNow.Date;
        DateTime end = start.AddDays(10);

        (_, _, List<Stage> stages, _) = await SeedGroupStageWithTeamsAsync(db, teamsPerGroup: 2, groupCount: 1, start, end);

        List<Match> matches = await matchService.CreateAutomatedMatchesAsync(stages[0].Id);

        Assert.Single(matches);
        DateTime expectedDate = start.AddDays((end - start).TotalDays / 2);
        Assert.Equal(expectedDate, matches[0].MatchDate);
    }

    /// <summary>
    /// teamsPerGroup=3 produces 3 round-robin matches (3*2/2 = 3).
    /// </summary>
    [Fact]
    public async Task CreateAutomatedMatchesAsync_GroupStage_MultipleMatches_SpreadEvenlyAcrossRange()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        DateTime start = DateTime.UtcNow.Date;
        DateTime end = start.AddDays(10);

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
    public async Task CreateAutomatedMatchesAsync_GroupStage_SingleGroupDivision_SeedsEachMatchWithARealPairing()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        (_, _, List<Stage> stages, List<Team> teams) = await SeedGroupStageWithTeamsAsync(db, teamsPerGroup: 4, groupCount: 1);

        List<Match> matches = await matchService.CreateAutomatedMatchesAsync(stages[0].Id);

        Assert.Equal(6, matches.Count);
        Assert.All(matches, m =>
        {
            Assert.NotNull(m.HomeTeamId);
            Assert.NotNull(m.VisitorTeamId);
            Assert.Contains(m.HomeTeamId!.Value, teams.Select(t => t.Id));
            Assert.Contains(m.VisitorTeamId!.Value, teams.Select(t => t.Id));
            Assert.NotEqual(m.HomeTeamId, m.VisitorTeamId);
        });

        foreach (Team team in teams)
        {
            int appearances = matches.Count(m => m.HomeTeamId == team.Id || m.VisitorTeamId == team.Id);
            Assert.Equal(3, appearances);
        }
    }

    /// <summary>
    /// Reproduces a real bug found while driving the tournament wizard
    /// end-to-end: a multi-zone tournament registers every selected team to
    /// the TOURNAMENT first, then assigns each zone's own subset to that
    /// zone's own Group stage (via AssignTeamsToStageAsync/StageTeamMatch)
    /// — exactly what submitWizard.ts does. Before this fix,
    /// ResolveGroupTeamCountAsync computed "registered teams ÷ groups in
    /// THIS division" (always ÷ 1, since each zone has exactly one Group
    /// stage), which equals the TOURNAMENT-WIDE team total, not this
    /// zone's assigned team count — so whenever the two coincidentally
    /// differed, matches were silently generated for the wrong pool
    /// (mixing in teams from every other zone) instead of the teams
    /// actually assigned to this stage.
    /// </summary>
    [Fact]
    public async Task CreateAutomatedMatchesAsync_GroupStage_ExplicitAssignmentDiffersFromTournamentTotal_UsesOnlyAssignedTeams()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        (Tournament tournament, Division zoneA) = await SeedDivisionAsync(db);
        Division zoneB = new()
        {
            Slug = $"division-{Guid.NewGuid()}",
            Name = $"Division-{Guid.NewGuid()}",
            Tournament = tournament,
            Stages = [],
            CreatedBy = "test",
        };
        db.Divisions.Add(zoneB);
        await db.SaveChangesAsync();

        // 8 teams registered to the TOURNAMENT as a whole (4 per zone) —
        // the wizard's "register every selected team up front" step.
        List<Team> zoneATeams = await SeedTeamsAsync(db, 4, tournament.Id);
        List<Team> zoneBTeams = await SeedTeamsAsync(db, 4, tournament.Id);

        List<Stage> zoneAStages = await SeedGroupStagesAsync(db, zoneA, groupCount: 1);
        List<Stage> zoneBStages = await SeedGroupStagesAsync(db, zoneB, groupCount: 1);
        Stage zoneAGroupStage = zoneAStages[0];

        // Only zoneA's own 4 teams are explicitly assigned to zoneA's group
        // stage (zoneB's stage is left unassigned, mirroring "the other
        // zones haven't been processed by the wizard yet").
        db.StageTeamMatches.AddRange(zoneATeams.Select(t => new StageTeamMatch
        {
            StageId = zoneAGroupStage.Id,
            TeamId = t.Id,
            CreatedBy = "test",
        }));
        await db.SaveChangesAsync();

        List<Match> matches = await matchService.CreateAutomatedMatchesAsync(zoneAGroupStage.Id);

        Assert.Equal(6, matches.Count);
        Assert.All(matches, m =>
        {
            Assert.NotNull(m.HomeTeamId);
            Assert.NotNull(m.VisitorTeamId);
            Assert.Contains(m.HomeTeamId!.Value, zoneATeams.Select(t => t.Id));
            Assert.Contains(m.VisitorTeamId!.Value, zoneATeams.Select(t => t.Id));
            Assert.DoesNotContain(m.HomeTeamId!.Value, zoneBTeams.Select(t => t.Id));
            Assert.DoesNotContain(m.VisitorTeamId!.Value, zoneBTeams.Select(t => t.Id));
        });

        // zoneB's stage existing (unassigned) is itself part of what makes
        // this reproduce the bug: it proves the fix isn't just "there's
        // only one Group stage in the tournament".
        Assert.Single(zoneBStages);
    }

    [Fact]
    public async Task CreateAutomatedMatchesAsync_GroupStage_MultipleGroupsInDivision_LeavesMatchesUnseeded()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        (_, _, List<Stage> stages, _) = await SeedGroupStageWithTeamsAsync(db, teamsPerGroup: 4, groupCount: 2);

        List<Match> matches = await matchService.CreateAutomatedMatchesAsync(stages[0].Id);

        Assert.All(matches, m => Assert.Null(m.HomeTeamId));
    }

    /// <summary>
    /// Every match built in one CreateAutomatedMatchesAsync call is unpersisted
    /// until the whole batch is saved together, so a naive uniqueness check
    /// against the repository alone would miss collisions between matches in
    /// the same batch. Forcing a single-day stage range collapses every
    /// unseeded match's slug source ("TBD vs TBD {date}") to the same base
    /// slug, proving the batch-local dedup in AssignMatchSlugsAsync actually
    /// catches it and resolves it via the usual -2/-3 suffixing.
    /// </summary>
    [Fact]
    public async Task CreateAutomatedMatchesAsync_GroupStage_UnseededMatchesShareSameDate_AssignsUniqueSlugsViaSuffix()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        DateTime sameDate = DateTime.UtcNow.Date;
        (_, _, List<Stage> stages, _) = await SeedGroupStageWithTeamsAsync(db, teamsPerGroup: 4, groupCount: 2, sameDate, sameDate);

        List<Match> matches = await matchService.CreateAutomatedMatchesAsync(stages[0].Id);

        // Assertions intentionally avoid pinning exact suffix values: this test
        // class shares one SQLite database across its fixture, so an earlier
        // test may already own the bare (unsuffixed) slug for "today" — the
        // point under test is that all 6 in-batch collisions resolve to
        // distinct slugs, not which exact suffix each one lands on.
        string expectedBasePrefix = $"tbd-vs-tbd-{sameDate:yyyy-MM-dd}";

        Assert.Equal(6, matches.Count);
        Assert.All(matches, m => Assert.StartsWith(expectedBasePrefix, m.Slug));
        Assert.Equal(matches.Count, matches.Select(m => m.Slug).Distinct().Count());
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

    private static async Task<List<Team>> SeedTeamsAsync(ApplicationDBContext db, int count, Guid? tournamentId = null)
    {
        List<Team> teams = [];
        for (int i = 0; i < count; i++)
        {
            teams.Add(new Team
            {
                Name = $"Team-{i}-{Guid.NewGuid()}",
                Slug = $"team-{i}-{Guid.NewGuid()}",
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
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = start.AddDays(-1),
            StartDate = start,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        Division division = new()
        {
            Slug = $"division-{Guid.NewGuid()}",
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
                Slug = $"stage-{Guid.NewGuid()}",
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
}
