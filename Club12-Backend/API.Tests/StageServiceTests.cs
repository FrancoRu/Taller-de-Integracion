using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces.Services;
using Application.Utils.Constants.Stage;
using Application.Utils.Helper.StageHelper;
using Domain.Entities.Models;
using Domain.Enums;
using Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.Tests;

/// <summary>
/// Characterization (approval) tests for <see cref="Application.Services.StageService"/>'s
/// automated stage-chain generation (<see cref="IStageService.CreateAutomatedStagesAsync"/>) and
/// team-assignment logic (<see cref="IStageService.AssignTeamsToStageAsync"/>). These tests pin
/// the currently-correct behavior of the existing, unmodified service — they are pure test
/// additions and assert no production code changes.
/// </summary>
public class StageServiceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public StageServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---------------------------------------------------------------
    // CreateAutomatedStagesAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task CreateAutomatedStagesAsync_EightTeams_CreatesTwoGroupsWithoutQuarterFinal()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        (Tournament tournament, _) = await SeedTournamentWithTeamsAsync(db, 8);
        Division division = await SeedDivisionAsync(db, tournament);

        List<Stage> stages = await stageService.CreateAutomatedStagesAsync(division.Id);

        Assert.Equal(5, stages.Count);
        Assert.DoesNotContain(stages, s => s.StageType == StageType.QuarterFinal);

        Stage groupA = Assert.Single(stages, s => s.Name == $"{StageTemplate.Group.Name} - Grupo A");
        Stage groupB = Assert.Single(stages, s => s.Name == $"{StageTemplate.Group.Name} - Grupo B");
        Stage semiFinal = Assert.Single(stages, s => s.StageType == StageType.SemiFinal);
        Stage thirdPlace = Assert.Single(stages, s => s.StageType == StageType.ThirdPlace);
        Stage final = Assert.Single(stages, s => s.StageType == StageType.Final);

        // Both groups run in parallel: identical start/end dates.
        Assert.Equal(tournament.StartDate, groupA.StartDate);
        Assert.Equal(tournament.StartDate, groupB.StartDate);
        Assert.Equal(tournament.StartDate.AddDays(StageTemplate.DurationDays * 2), groupA.EndDate);
        Assert.Equal(groupA.EndDate, groupB.EndDate);

        // Documented chain gaps taken from StageService.CreateAutomatedStagesAsync.
        Assert.Equal(groupA.EndDate.AddDays(2), semiFinal.StartDate);
        Assert.Equal(semiFinal.StartDate.AddDays(StageTemplate.DurationDays), semiFinal.EndDate);
        Assert.Equal(semiFinal.EndDate.AddDays(1), thirdPlace.StartDate);
        Assert.Equal(thirdPlace.StartDate.AddDays(StageTemplate.DurationDays), thirdPlace.EndDate);
        Assert.Equal(thirdPlace.EndDate.AddDays(2), final.StartDate);
        Assert.Equal(final.StartDate.AddDays(StageTemplate.DurationDays), final.EndDate);

        Assert.Equal(0, groupA.Order);
        Assert.Equal(1, groupB.Order);
        Assert.Equal(2, semiFinal.Order);
        Assert.Equal(3, thirdPlace.Order);
        Assert.Equal(4, final.Order);
    }

    public static readonly TheoryData<int, int> ValidSizesWithQuarterFinal = new()
    {
        { 16, 4 },
        { 32, 8 },
        { 64, 16 },
    };

    [Theory]
    [MemberData(nameof(ValidSizesWithQuarterFinal))]
    public async Task CreateAutomatedStagesAsync_ValidSizesWithQuarterFinal_CreatesExpectedGroupsAndChain(
        int teamCount, int expectedGroupCount)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        (Tournament tournament, _) = await SeedTournamentWithTeamsAsync(db, teamCount);
        Division division = await SeedDivisionAsync(db, tournament);

        List<Stage> stages = await stageService.CreateAutomatedStagesAsync(division.Id);

        Assert.Equal(expectedGroupCount + 4, stages.Count);

        List<Stage> groupStages = [.. stages.Where(s => s.StageType == StageType.Group).OrderBy(s => s.Order)];
        Assert.Equal(expectedGroupCount, groupStages.Count);

        for (int i = 0; i < expectedGroupCount; i++)
        {
            char expectedLetter = (char)('A' + i);
            Assert.Equal($"{StageTemplate.Group.Name} - Grupo {expectedLetter}", groupStages[i].Name);
            Assert.Equal(i, groupStages[i].Order);
            Assert.Equal(tournament.StartDate, groupStages[i].StartDate);
        }

        Stage quarterFinal = Assert.Single(stages, s => s.StageType == StageType.QuarterFinal);
        Stage semiFinal = Assert.Single(stages, s => s.StageType == StageType.SemiFinal);
        Stage thirdPlace = Assert.Single(stages, s => s.StageType == StageType.ThirdPlace);
        Stage final = Assert.Single(stages, s => s.StageType == StageType.Final);

        Assert.True(quarterFinal.Order < semiFinal.Order);
        Assert.True(semiFinal.Order < thirdPlace.Order);
        Assert.True(thirdPlace.Order < final.Order);

        DateTime groupEnd = groupStages[0].EndDate;
        Assert.Equal(groupEnd.AddDays(2), quarterFinal.StartDate);
        Assert.Equal(quarterFinal.StartDate.AddDays(StageTemplate.DurationDays), quarterFinal.EndDate);
        Assert.Equal(quarterFinal.EndDate.AddDays(2), semiFinal.StartDate);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(12)]
    public async Task CreateAutomatedStagesAsync_InvalidTeamCount_ThrowsAndCreatesNoStages(int teamCount)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        (Tournament tournament, _) = await SeedTournamentWithTeamsAsync(db, teamCount);
        Division division = await SeedDivisionAsync(db, tournament);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.CreateAutomatedStagesAsync(division.Id));

        int stageCount = await db.Stages.CountAsync(s => s.DivisionId == division.Id);
        Assert.Equal(0, stageCount);
    }

    [Fact]
    public async Task CreateAutomatedStagesAsync_DivisionNotFound_Throws()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.CreateAutomatedStagesAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAutomatedStagesAsync_DivisionAlreadyHasStages_Throws()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        (Tournament tournament, _) = await SeedTournamentWithTeamsAsync(db, 8);
        Division division = await SeedDivisionAsync(db, tournament, withStages: true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.CreateAutomatedStagesAsync(division.Id));
    }

    // ---------------------------------------------------------------
    // AssignTeamsToStageAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task AssignTeamsToStageAsync_ExactSlotMatch_AssignsAllTeams()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        (Stage stage, Tournament tournament, _) = await SeedStageWithSlotsAsync(db, StageType.SemiFinal, existingAssignmentCount: 0);
        List<Team> poolTeams = await SeedTeamsAsync(db, tournament, 4);
        List<Guid> teamIds = [.. poolTeams.Select(t => t.Id)];

        await stageService.AssignTeamsToStageAsync(stage, teamIds, auto: false);

        List<StageTeamMatch> records = await db.StageTeamMatches.Where(stm => stm.StageId == stage.Id).ToListAsync();
        Assert.Equal(4, records.Count);

        List<Guid> expectedIds = [.. teamIds.OrderBy(id => id)];
        List<Guid> actualIds = [.. records.Select(r => r.TeamId).OrderBy(id => id)];
        Assert.Equal(expectedIds, actualIds);
    }

    [Fact]
    public async Task AssignTeamsToStageAsync_TooManyTeamsForSlots_ThrowsAndCreatesNoRecords()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        // ThirdPlace capacity is 2; 1 existing assignment leaves exactly 1 available slot.
        (Stage stage, Tournament tournament, _) = await SeedStageWithSlotsAsync(db, StageType.ThirdPlace, existingAssignmentCount: 1);
        List<Team> poolTeams = await SeedTeamsAsync(db, tournament, 3);
        List<Guid> teamIds = [.. poolTeams.Select(t => t.Id)];

        await Assert.ThrowsAsync<Exception>(
            () => stageService.AssignTeamsToStageAsync(stage, teamIds, auto: false));

        int recordCount = await db.StageTeamMatches.CountAsync(stm => stm.StageId == stage.Id);
        Assert.Equal(1, recordCount);
    }

    [Fact]
    public async Task AssignTeamsToStageAsync_FewerTeamsThanSlots_LeavesSlotsAvailable()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        (Stage stage, Tournament tournament, _) = await SeedStageWithSlotsAsync(db, StageType.SemiFinal, existingAssignmentCount: 0);
        List<Team> poolTeams = await SeedTeamsAsync(db, tournament, 2);
        List<Guid> teamIds = [.. poolTeams.Select(t => t.Id)];

        await stageService.AssignTeamsToStageAsync(stage, teamIds, auto: false);

        int recordCount = await db.StageTeamMatches.CountAsync(stm => stm.StageId == stage.Id);
        Assert.Equal(2, recordCount);

        int maxTeams = StageHelper.GetMaxTeamsForStage(stage.StageType);
        Assert.Equal(2, maxTeams - recordCount);
    }

    [Fact]
    public async Task AssignTeamsToStageAsync_StageAlreadyAtCapacity_ThrowsForManualAndAuto()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        // ThirdPlace capacity is 2; 2 existing assignments fill the stage.
        (Stage stage, Tournament tournament, _) = await SeedStageWithSlotsAsync(db, StageType.ThirdPlace, existingAssignmentCount: 2);
        List<Team> poolTeams = await SeedTeamsAsync(db, tournament, 1);
        List<Guid> teamIds = [.. poolTeams.Select(t => t.Id)];

        await Assert.ThrowsAsync<Exception>(
            () => stageService.AssignTeamsToStageAsync(stage, teamIds, auto: false));
        await Assert.ThrowsAsync<Exception>(
            () => stageService.AssignTeamsToStageAsync(stage, null, auto: true));

        int recordCount = await db.StageTeamMatches.CountAsync(stm => stm.StageId == stage.Id);
        Assert.Equal(2, recordCount);
    }

    [Fact]
    public async Task AssignTeamsToStageAsync_DuplicateAndAlreadyAssignedIds_AreFilteredOut()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        // SemiFinal capacity is 4; 1 existing assignment leaves 3 available slots.
        (Stage stage, Tournament tournament, List<Guid> existingIds) = await SeedStageWithSlotsAsync(db, StageType.SemiFinal, existingAssignmentCount: 1);
        List<Team> poolTeams = await SeedTeamsAsync(db, tournament, 2);

        List<Guid> requestIds = [poolTeams[0].Id, poolTeams[0].Id, poolTeams[1].Id, existingIds[0]];

        await stageService.AssignTeamsToStageAsync(stage, requestIds, auto: false);

        List<StageTeamMatch> records = await db.StageTeamMatches.Where(stm => stm.StageId == stage.Id).ToListAsync();
        Assert.Equal(3, records.Count);
        Assert.Contains(records, r => r.TeamId == poolTeams[0].Id);
        Assert.Contains(records, r => r.TeamId == poolTeams[1].Id);
        Assert.Single(records, r => r.TeamId == existingIds[0]);
    }

    [Fact]
    public async Task AssignTeamsToStageAsync_AutoMode_AssignsUpToAvailableSlots()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        // SemiFinal capacity is 4; 1 existing assignment leaves 3 available slots.
        (Stage stage, Tournament tournament, List<Guid> existingIds) = await SeedStageWithSlotsAsync(db, StageType.SemiFinal, existingAssignmentCount: 1);
        await SeedTeamsAsync(db, tournament, 5); // ensure eligible pool >= available slots

        await stageService.AssignTeamsToStageAsync(stage, null, auto: true);

        List<StageTeamMatch> records = await db.StageTeamMatches.Where(stm => stm.StageId == stage.Id).ToListAsync();
        Assert.Equal(4, records.Count);

        List<Guid> newlyAssigned = [.. records.Select(r => r.TeamId).Except(existingIds)];
        Assert.Equal(3, newlyAssigned.Count);
        Assert.DoesNotContain(existingIds[0], newlyAssigned);
    }

    // ---------------------------------------------------------------
    // Seed helpers (local to this file — no shared/production code)
    // ---------------------------------------------------------------

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db, int maxTeams = 64)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Characterization test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = startDate.AddDays(-1),
            StartDate = startDate,
            MaxTeams = maxTeams,
            MinTeams = 2,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        return tournament;
    }

    private static async Task<List<Team>> SeedTeamsAsync(ApplicationDBContext db, Tournament tournament, int count)
    {
        List<Team> teams = [];

        for (int i = 0; i < count; i++)
        {
            Team team = new()
            {
                Name = $"Team-{Guid.NewGuid()}",
                ThreeLetterCode = Guid.NewGuid().ToString("N")[..3].ToUpperInvariant(),
                LogoUrl = "http://example.com/logo.png",
                ShirtColor = "Red",
                TournamentId = tournament.Id,
                Players = [],
                CreatedBy = "test",
            };

            db.Teams.Add(team);
            teams.Add(team);
        }

        await db.SaveChangesAsync();

        return teams;
    }

    private static async Task<(Tournament tournament, List<Team> teams)> SeedTournamentWithTeamsAsync(ApplicationDBContext db, int teamCount)
    {
        Tournament tournament = await SeedTournamentAsync(db, Math.Max(teamCount, 2));
        List<Team> teams = await SeedTeamsAsync(db, tournament, teamCount);

        return (tournament, teams);
    }

    private static async Task<Division> SeedDivisionAsync(ApplicationDBContext db, Tournament tournament, bool withStages = false)
    {
        Division division = new()
        {
            Name = $"Division-{Guid.NewGuid()}",
            Tournament = tournament,
            TournamentId = tournament.Id,
            Stages = [],
            CreatedBy = "test",
        };

        db.Divisions.Add(division);
        await db.SaveChangesAsync();

        if (withStages)
        {
            Stage existingStage = new()
            {
                Name = $"Existing-{Guid.NewGuid()}",
                StageType = StageType.Group,
                IsActive = true,
                StartDate = tournament.StartDate,
                EndDate = tournament.StartDate.AddDays(StageTemplate.DurationDays),
                DivisionId = division.Id,
                Division = division,
                Matches = [],
                CreatedBy = "test",
            };

            db.Stages.Add(existingStage);
            await db.SaveChangesAsync();
        }

        return division;
    }

    private static async Task<(Stage stage, Tournament tournament, List<Guid> existingTeamIds)> SeedStageWithSlotsAsync(
        ApplicationDBContext db, StageType stageType, int existingAssignmentCount)
    {
        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);

        Stage stage = new()
        {
            Name = $"Stage-{Guid.NewGuid()}",
            StageType = stageType,
            IsActive = true,
            StartDate = tournament.StartDate,
            EndDate = tournament.StartDate.AddDays(StageTemplate.DurationDays),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        db.Stages.Add(stage);
        await db.SaveChangesAsync();

        List<Guid> existingTeamIds = [];

        if (existingAssignmentCount > 0)
        {
            List<Team> existingTeams = await SeedTeamsAsync(db, tournament, existingAssignmentCount);

            foreach (Team team in existingTeams)
            {
                db.StageTeamMatches.Add(new StageTeamMatch
                {
                    StageId = stage.Id,
                    TeamId = team.Id,
                    CreatedBy = "test",
                });
                existingTeamIds.Add(team.Id);
            }

            await db.SaveChangesAsync();
        }

        return (stage, tournament, existingTeamIds);
    }
}
