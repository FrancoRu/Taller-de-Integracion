using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces.Services;
using Domain.Entities.Models;
using Domain.Enums;
using Infrastructure.Persistance;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
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
}
