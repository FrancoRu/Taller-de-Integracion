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
    /// HU-65: a group-stage fixture is scheduled by matchday, one round per
    /// Sunday from the stage start — not by spreading matches evenly across the
    /// stage's date range. teamsPerGroup=2 produces a single round-robin match
    /// (2*1/2 = 1) in round 1, dated the first Sunday on or after the start.
    /// </summary>
    [Fact]
    public async Task CreateAutomatedMatchesAsync_GroupStage_SingleMatch_IsRoundOneOnFirstSunday()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        DateTime start = DateTime.UtcNow.Date;
        DateTime end = start.AddDays(10);

        (_, _, List<Stage> stages, _) = await SeedGroupStageWithTeamsAsync(db, teamsPerGroup: 2, groupCount: 1, start, end);

        List<Match> matches = await matchService.CreateAutomatedMatchesAsync(stages[0].Id);

        Assert.Single(matches);
        Assert.Equal(1, matches[0].Round);
        Assert.Equal(FirstSundayOnOrAfter(start), matches[0].MatchDate);
    }

    /// <summary>
    /// HU-63/HU-65: teamsPerGroup=3 is odd, so the single round-robin has 3
    /// rounds (each with one match and one idle team), scheduled on 3
    /// consecutive Sundays. Round is the canonical grouping — one match per
    /// round, one round per Sunday.
    /// </summary>
    [Fact]
    public async Task CreateAutomatedMatchesAsync_GroupStage_OddTeams_OneMatchPerRoundOnConsecutiveSundays()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        DateTime start = DateTime.UtcNow.Date;
        DateTime end = start.AddDays(30);

        (_, _, List<Stage> stages, _) = await SeedGroupStageWithTeamsAsync(db, teamsPerGroup: 3, groupCount: 1, start, end);

        List<Match> matches = await matchService.CreateAutomatedMatchesAsync(stages[0].Id);
        List<Match> ordered = [.. matches.OrderBy(m => m.Round)];

        Assert.Equal(3, ordered.Count);
        Assert.Equal([1, 2, 3], [.. ordered.Select(m => m.Round)]);

        DateTime firstSunday = FirstSundayOnOrAfter(start);
        for (int i = 0; i < ordered.Count; i++)
        {
            Assert.Equal(firstSunday.AddDays(7 * i), ordered[i].MatchDate);
        }
    }

    /// <summary>
    /// The default Sunday schedule for a round: the first Sunday on or after
    /// the start date. Mirrors RoundCalendar so tests assert against the same
    /// contract the service uses.
    /// </summary>
    private static DateTime FirstSundayOnOrAfter(DateTime start)
    {
        int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)start.Date.DayOfWeek + 7) % 7;
        return start.Date.AddDays(daysUntilSunday);
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

        // Unseeded matches still carry the round structure (HU-63): 4 teams =>
        // 3 rounds of 2 unseeded ("TBD vs TBD") matches each. The two matches
        // that share a round share a Sunday date, so their base slug collides —
        // the point under test is that all in-batch collisions resolve to
        // distinct slugs. Assertions avoid pinning exact suffix values because
        // this test class shares one SQLite database across its fixture.
        Assert.Equal(6, matches.Count);
        Assert.All(matches, m => Assert.NotNull(m.Round));
        Assert.All(matches, m => Assert.StartsWith("tbd-vs-tbd-", m.Slug));
        Assert.Equal(matches.Count, matches.Select(m => m.Slug).Distinct().Count());
    }

    /// <summary>
    /// HU-65/HU-67: a group-stage fixture is scheduled from the start date by
    /// consecutive Sundays and does not depend on the stage end date, so an
    /// inverted range no longer aborts generation the way the old even-spread
    /// date distribution did — the match is simply placed on the first Sunday
    /// on or after the start.
    /// </summary>
    [Fact]
    public async Task CreateAutomatedMatchesAsync_GroupStage_EndDateBeforeStartDate_StillSchedulesFromStartSunday()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        DateTime start = DateTime.UtcNow.Date;
        DateTime end = start.AddDays(-1);

        (_, _, List<Stage> stages, _) = await SeedGroupStageWithTeamsAsync(db, teamsPerGroup: 2, groupCount: 1, start, end);

        List<Match> matches = await matchService.CreateAutomatedMatchesAsync(stages[0].Id);

        Assert.Single(matches);
        Assert.Equal(1, matches[0].Round);
        Assert.Equal(FirstSundayOnOrAfter(start), matches[0].MatchDate);
    }

    /// <summary>
    /// HU-68/HU-67: suspending (and rescheduling) a match marks it Suspended and
    /// moves its date, but never changes its round nor the rest of the fixture's
    /// rounds. Every other match keeps the round it was generated with.
    /// </summary>
    [Fact]
    public async Task SuspendMatchAsync_MarksSuspendedAndMovesDate_ButKeepsRoundAndRestOfFixture()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        (_, _, List<Stage> stages, _) = await SeedGroupStageWithTeamsAsync(db, teamsPerGroup: 4, groupCount: 1);

        List<Match> matches = await matchService.CreateAutomatedMatchesAsync(stages[0].Id);
        Match target = matches[0];
        int originalRound = target.Round!.Value;

        // Snapshot every other match's round to prove the fixture is untouched.
        Dictionary<Guid, int?> otherRoundsBefore = matches
            .Where(m => m.Id != target.Id)
            .ToDictionary(m => m.Id, m => m.Round);

        DateTime newDate = target.MatchDate.AddDays(3);
        Match? suspended = await matchService.SuspendMatchAsync(target.Id, newDate);

        Assert.NotNull(suspended);
        Assert.Equal(MatchStatus.Suspended, suspended!.Status);
        Assert.Equal(newDate, suspended.MatchDate);
        Assert.Equal(originalRound, suspended.Round);

        // Re-read from the database to confirm the round was persisted intact.
        Match? reloaded = await matchService.GetMatchByIdAsync(target.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(originalRound, reloaded!.Round);
        Assert.Equal(MatchStatus.Suspended, reloaded.Status);

        List<Match> byRound = await matchService.GetStageMatchesByRoundAsync(stages[0].Id);
        foreach (Match other in byRound.Where(m => m.Id != target.Id))
        {
            Assert.Equal(otherRoundsBefore[other.Id], other.Round);
        }
    }

    /// <summary>
    /// HU-63: a stage's matches can be fetched grouped/ordered by round so the
    /// frontend renders "Fecha 1 / Partido…, Fecha 2 / …". Ordering is by round,
    /// not calendar date.
    /// </summary>
    [Fact]
    public async Task GetStageMatchesByRoundAsync_OrdersMatchesByRound()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        (_, _, List<Stage> stages, _) = await SeedGroupStageWithTeamsAsync(db, teamsPerGroup: 4, groupCount: 1);
        await matchService.CreateAutomatedMatchesAsync(stages[0].Id);

        List<Match> byRound = await matchService.GetStageMatchesByRoundAsync(stages[0].Id);

        Assert.NotEmpty(byRound);
        Assert.All(byRound, m => Assert.NotNull(m.Round));

        // 4 teams => 3 rounds of 2 matches each, delivered round 1, 2, 3.
        List<int> rounds = [.. byRound.Select(m => m.Round!.Value)];
        Assert.Equal([1, 1, 2, 2, 3, 3], rounds);
    }

    /// <summary>
    /// HU-111: a group-stage fixture is laid out by jornada, weekly. Every match
    /// sharing a <see cref="Match.Round"/> is dated the same calendar day, and
    /// successive rounds are exactly seven days apart. Dates only: the time
    /// component is a neutral midnight and no venue is assigned, so the admin
    /// fills real time/venue later (HU-66/HU-67).
    /// </summary>
    [Fact]
    public async Task CreateAutomatedMatchesAsync_GroupStage_SameRoundSharesDate_RoundsAreWeekly_NeutralTimeNoVenue()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        DateTime start = new(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc); // Fixed anchor keeps the assertions deterministic.
        (_, _, List<Stage> stages, _) = await SeedGroupStageWithTeamsAsync(db, teamsPerGroup: 4, groupCount: 1, start, start.AddDays(60));

        List<Match> matches = await matchService.CreateAutomatedMatchesAsync(stages[0].Id);

        // 4 teams => 3 rounds of 2 matches each.
        List<IGrouping<int, Match>> byRound = [.. matches.GroupBy(m => m.Round!.Value).OrderBy(g => g.Key)];
        Assert.Equal(3, byRound.Count);

        // Every match in a round shares exactly one calendar date.
        Assert.All(byRound, group => Assert.Single(group.Select(m => m.MatchDate).Distinct()));

        // Successive rounds are exactly one week apart.
        List<DateTime> roundDates = [.. byRound.Select(group => group.First().MatchDate)];
        for (int i = 0; i < roundDates.Count; i++)
        {
            Assert.Equal(roundDates[0].AddDays(7 * i), roundDates[i]);
        }

        // Dates only: neutral midnight time, no venue.
        Assert.All(matches, m => Assert.Equal(TimeSpan.Zero, m.MatchDate.TimeOfDay));
        Assert.All(matches, m => Assert.Null(m.VenueId));
    }

    /// <summary>
    /// HU-111 core anti-collision guarantee: a team belongs to its zone AND the
    /// cross-division cup, so the two fixtures must never place a jornada on the
    /// same day. Regular zones are dated on Sundays; the cross cup is shifted to
    /// a different fixed weekday (Wednesday), so no calendar date is ever shared
    /// between the two fixtures on any jornada.
    /// </summary>
    [Fact]
    public async Task CreateAutomatedMatchesAsync_RegularZoneAndCrossCup_SameTournament_NeverShareADate()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IMatchService matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

        DateTime start = new(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc); // Fixed anchor keeps the assertions deterministic.

        (Tournament tournament, Division zone) = await SeedDivisionAsync(db);
        Stage zoneStage = (await SeedGroupStagesAsync(db, zone, groupCount: 1, start, start.AddDays(60)))[0];
        List<Team> zoneTeams = await SeedTeamsAsync(db, 4, tournament.Id);
        await AssignTeamsToStageAsync(db, zoneStage, zoneTeams);

        Division cup = await SeedCrossCupDivisionAsync(db, tournament);
        Stage cupStage = (await SeedGroupStagesAsync(db, cup, groupCount: 1, start, start.AddDays(60)))[0];
        List<Team> cupTeams = await SeedTeamsAsync(db, 4, tournament.Id);
        await AssignTeamsToStageAsync(db, cupStage, cupTeams);

        List<Match> zoneMatches = await matchService.CreateAutomatedMatchesAsync(zoneStage.Id);
        List<Match> cupMatches = await matchService.CreateAutomatedMatchesAsync(cupStage.Id);

        // Zones land on Sundays; the cross cup on a different fixed weekday.
        Assert.NotEmpty(zoneMatches);
        Assert.NotEmpty(cupMatches);
        Assert.All(zoneMatches, m => Assert.Equal(DayOfWeek.Sunday, m.MatchDate.DayOfWeek));
        Assert.All(cupMatches, m => Assert.Equal(DayOfWeek.Wednesday, m.MatchDate.DayOfWeek));

        // The core guarantee: no jornada of either fixture ever shares a date.
        HashSet<DateTime> zoneDates = [.. zoneMatches.Select(m => m.MatchDate)];
        HashSet<DateTime> cupDates = [.. cupMatches.Select(m => m.MatchDate)];
        Assert.Empty(zoneDates.Intersect(cupDates));
    }

    private static async Task<Division> SeedCrossCupDivisionAsync(ApplicationDBContext db, Tournament tournament)
    {
        Division cup = new()
        {
            Slug = $"cross-cup-{Guid.NewGuid()}",
            Name = $"Cross-Cup-{Guid.NewGuid()}",
            Tournament = tournament,
            TournamentId = tournament.Id,
            IsCrossDivisionCup = true,
            Stages = [],
            CreatedBy = "test",
        };

        db.Divisions.Add(cup);
        await db.SaveChangesAsync();

        return cup;
    }

    private static async Task AssignTeamsToStageAsync(ApplicationDBContext db, Stage stage, List<Team> teams)
    {
        db.StageTeamMatches.AddRange(teams.Select(t => new StageTeamMatch
        {
            StageId = stage.Id,
            TeamId = t.Id,
            CreatedBy = "test",
        }));
        await db.SaveChangesAsync();
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
