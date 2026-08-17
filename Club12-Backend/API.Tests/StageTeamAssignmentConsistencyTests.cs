using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Guards against a team being assigned to two competitive divisions of
/// the same tournament at once — the exact inconsistent state flagged
/// during this session's business-logic audit. Cross-division-cup
/// divisions (e.g. "Copa Club12") are deliberately exempt: a team is
/// expected to belong to both its zone and the cup at the same time.
/// </summary>
public class StageTeamAssignmentConsistencyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public StageTeamAssignmentConsistencyTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AssignTeamsToStageAsync_TeamAlreadyInAnotherDivisionOfSameTournament_Throws()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 1);
        Division zoneA = await SeedDivisionAsync(db, tournament, isCrossDivisionCup: false);
        Division zoneB = await SeedDivisionAsync(db, tournament, isCrossDivisionCup: false);
        Stage zoneAStage = await SeedStageAsync(db, zoneA, tournament);
        Stage zoneBStage = await SeedStageAsync(db, zoneB, tournament);

        await stageService.AssignTeamsToStageAsync(zoneAStage, [teams[0].Id], auto: false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => stageService.AssignTeamsToStageAsync(zoneBStage, [teams[0].Id], auto: false));

        int zoneBRecordCount = await db.StageTeamMatches.CountAsync(stm => stm.StageId == zoneBStage.Id);
        Assert.Equal(0, zoneBRecordCount);
    }

    [Fact]
    public async Task AssignTeamsToStageAsync_TeamAlreadyInHomeZone_CanAlsoJoinCrossDivisionCup()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournament = await SeedTournamentAsync(db);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 1);
        Division zoneA = await SeedDivisionAsync(db, tournament, isCrossDivisionCup: false);
        Division copaClub12 = await SeedDivisionAsync(db, tournament, isCrossDivisionCup: true);
        Stage zoneAStage = await SeedStageAsync(db, zoneA, tournament);
        Stage copaStage = await SeedStageAsync(db, copaClub12, tournament);

        await stageService.AssignTeamsToStageAsync(zoneAStage, [teams[0].Id], auto: false);
        await stageService.AssignTeamsToStageAsync(copaStage, [teams[0].Id], auto: false);

        int copaRecordCount = await db.StageTeamMatches.CountAsync(stm => stm.StageId == copaStage.Id && stm.TeamId == teams[0].Id);
        Assert.Equal(1, copaRecordCount);
    }

    [Fact]
    public async Task AssignTeamsToStageAsync_TeamFromDifferentTournament_DoesNotConflict()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IStageService stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

        Tournament tournamentOne = await SeedTournamentAsync(db);
        Tournament tournamentTwo = await SeedTournamentAsync(db);
        List<Team> teamsInTournamentOne = await SeedTeamsAsync(db, tournamentOne, 1);
        Division divisionInTournamentOne = await SeedDivisionAsync(db, tournamentOne, isCrossDivisionCup: false);
        Division divisionInTournamentTwo = await SeedDivisionAsync(db, tournamentTwo, isCrossDivisionCup: false);
        Stage stageInTournamentOne = await SeedStageAsync(db, divisionInTournamentOne, tournamentOne);
        Stage stageInTournamentTwo = await SeedStageAsync(db, divisionInTournamentTwo, tournamentTwo);

        await stageService.AssignTeamsToStageAsync(stageInTournamentOne, [teamsInTournamentOne[0].Id], auto: false);

        await stageService.AssignTeamsToStageAsync(stageInTournamentTwo, [teamsInTournamentOne[0].Id], auto: false);

        int recordCount = await db.StageTeamMatches.CountAsync(stm => stm.StageId == stageInTournamentTwo.Id);
        Assert.Equal(1, recordCount);
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Consistency test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = startDate.AddDays(-1),
            StartDate = startDate,
            MaxTeams = 32,
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
                Slug = $"team-{Guid.NewGuid()}",
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

    private static async Task<Division> SeedDivisionAsync(ApplicationDBContext db, Tournament tournament, bool isCrossDivisionCup)
    {
        Division division = new()
        {
            Name = $"Division-{Guid.NewGuid()}",
            Tournament = tournament,
            TournamentId = tournament.Id,
            IsCrossDivisionCup = isCrossDivisionCup,
            Stages = [],
            CreatedBy = "test",
        };

        db.Divisions.Add(division);
        await db.SaveChangesAsync();

        return division;
    }

    private static async Task<Stage> SeedStageAsync(ApplicationDBContext db, Division division, Tournament tournament)
    {
        Stage stage = new()
        {
            Name = $"Stage-{Guid.NewGuid()}",
            StageType = StageType.Group,
            IsActive = true,
            StartDate = tournament.StartDate,
            EndDate = tournament.StartDate.AddDays(7),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        db.Stages.Add(stage);
        await db.SaveChangesAsync();

        return stage;
    }
}
