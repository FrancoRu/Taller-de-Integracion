using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Covers IDivisionRosterService: roster CRUD, the idempotent enroll skip, the
/// one-regular-zone-plus-optional-cross-cup conflict rule, and the cascade
/// unenroll that removes a team's stage placements before its registration.
/// </summary>
public class DivisionRosterServiceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DivisionRosterServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EnrollTeamsAsync_NewTeams_CreatesRegistrations()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionRosterService rosterService = scope.ServiceProvider.GetRequiredService<IDivisionRosterService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 2);

        List<DivisionTeamRegistration> created = await rosterService.EnrollTeamsAsync(
            division.Id, [.. teams.Select(t => t.Id)]);

        Assert.Equal(2, created.Count);
        int rowCount = await db.DivisionTeamRegistrations.CountAsync(r => r.DivisionId == division.Id);
        Assert.Equal(2, rowCount);
    }

    [Fact]
    public async Task EnrollTeamsAsync_AlreadyRegisteredTeam_IsIdempotent_NoDuplicate()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionRosterService rosterService = scope.ServiceProvider.GetRequiredService<IDivisionRosterService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        Team team = (await SeedTeamsAsync(db, tournament, 1))[0];
        await SeedRegistrationAsync(db, division, team);

        await rosterService.EnrollTeamsAsync(division.Id, [team.Id]);

        int rowCount = await db.DivisionTeamRegistrations.CountAsync(
            r => r.DivisionId == division.Id && r.TeamId == team.Id);
        Assert.Equal(1, rowCount);
    }

    [Fact]
    public async Task EnrollTeamsAsync_TeamAlreadyInAnotherRegularDivision_ThrowsAndCreatesNoRegistration()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionRosterService rosterService = scope.ServiceProvider.GetRequiredService<IDivisionRosterService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division divisionA = await SeedDivisionAsync(db, tournament);
        Division divisionB = await SeedDivisionAsync(db, tournament);
        Team team = (await SeedTeamsAsync(db, tournament, 1))[0];
        await SeedRegistrationAsync(db, divisionA, team);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => rosterService.EnrollTeamsAsync(divisionB.Id, [team.Id]));

        int rowCount = await db.DivisionTeamRegistrations.CountAsync(r => r.DivisionId == divisionB.Id);
        Assert.Equal(0, rowCount);
    }

    [Fact]
    public async Task EnrollTeamsAsync_TeamInRegularDivisionPlusCrossCupDivision_BothRegistrationsSucceed()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionRosterService rosterService = scope.ServiceProvider.GetRequiredService<IDivisionRosterService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division regularDivision = await SeedDivisionAsync(db, tournament);
        Division cupDivision = await SeedDivisionAsync(db, tournament, isCrossDivisionCup: true);
        Team team = (await SeedTeamsAsync(db, tournament, 1))[0];
        await SeedRegistrationAsync(db, regularDivision, team);

        await rosterService.EnrollTeamsAsync(cupDivision.Id, [team.Id]);

        int rowCount = await db.DivisionTeamRegistrations.CountAsync(r => r.TeamId == team.Id);
        Assert.Equal(2, rowCount);
    }

    [Fact]
    public async Task EnrollTeamsAsync_SecondCrossCupRegistration_Throws()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionRosterService rosterService = scope.ServiceProvider.GetRequiredService<IDivisionRosterService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division firstCup = await SeedDivisionAsync(db, tournament, isCrossDivisionCup: true);
        Division secondCup = await SeedDivisionAsync(db, tournament, isCrossDivisionCup: true);
        Team team = (await SeedTeamsAsync(db, tournament, 1))[0];
        await SeedRegistrationAsync(db, firstCup, team);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => rosterService.EnrollTeamsAsync(secondCup.Id, [team.Id]));

        int rowCount = await db.DivisionTeamRegistrations.CountAsync(r => r.DivisionId == secondCup.Id);
        Assert.Equal(0, rowCount);
    }

    [Fact]
    public async Task EnrollTeamsAsync_TournamentStructureLocked_Throws()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionRosterService rosterService = scope.ServiceProvider.GetRequiredService<IDivisionRosterService>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.Ongoing);
        Division division = await SeedDivisionAsync(db, tournament);
        Team team = (await SeedTeamsAsync(db, tournament, 1))[0];

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => rosterService.EnrollTeamsAsync(division.Id, [team.Id]));

        int rowCount = await db.DivisionTeamRegistrations.CountAsync(r => r.DivisionId == division.Id);
        Assert.Equal(0, rowCount);
    }

    [Fact]
    public async Task UnenrollTeamsAsync_TeamStillPlacedInStage_RemovesPlacementThenRegistration()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionRosterService rosterService = scope.ServiceProvider.GetRequiredService<IDivisionRosterService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage stage = await SeedStageAsync(db, division, StageType.Group, "Grupo A");
        Team team = (await SeedTeamsAsync(db, tournament, 1))[0];
        await SeedRegistrationAsync(db, division, team);
        await SeedStageTeamMatchAsync(db, stage, team);

        await rosterService.UnenrollTeamsAsync(division.Id, [team.Id]);

        int placementCount = await db.StageTeamMatches.CountAsync(stm => stm.TeamId == team.Id);
        int registrationCount = await db.DivisionTeamRegistrations.CountAsync(
            r => r.DivisionId == division.Id && r.TeamId == team.Id);
        Assert.Equal(0, placementCount);
        Assert.Equal(0, registrationCount);
    }

    [Fact]
    public async Task UnenrollTeamsAsync_TeamNotPlaced_RemovesRegistrationOnly()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionRosterService rosterService = scope.ServiceProvider.GetRequiredService<IDivisionRosterService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        Team team = (await SeedTeamsAsync(db, tournament, 1))[0];
        await SeedRegistrationAsync(db, division, team);

        await rosterService.UnenrollTeamsAsync(division.Id, [team.Id]);

        int registrationCount = await db.DivisionTeamRegistrations.CountAsync(
            r => r.DivisionId == division.Id && r.TeamId == team.Id);
        Assert.Equal(0, registrationCount);
    }

    /// <summary>
    /// The roster edit lock mirrors StageService's structure lock so a locked
    /// tournament can't be unenrolled from underneath its already-generated matches.
    /// </summary>
    [Fact]
    public async Task UnenrollTeamsAsync_TournamentStructureLocked_Throws()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionRosterService rosterService = scope.ServiceProvider.GetRequiredService<IDivisionRosterService>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.Ongoing);
        Division division = await SeedDivisionAsync(db, tournament);
        Team team = (await SeedTeamsAsync(db, tournament, 1))[0];
        await SeedRegistrationAsync(db, division, team);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => rosterService.UnenrollTeamsAsync(division.Id, [team.Id]));

        int registrationCount = await db.DivisionTeamRegistrations.CountAsync(
            r => r.DivisionId == division.Id && r.TeamId == team.Id);
        Assert.Equal(1, registrationCount);
    }

    [Fact]
    public async Task GetRosterAsync_ReturnsAllEnrolledTeams_IncludingUnplacedOnes()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionRosterService rosterService = scope.ServiceProvider.GetRequiredService<IDivisionRosterService>();

        Tournament tournament = await SeedTournamentAsync(db);
        Division division = await SeedDivisionAsync(db, tournament);
        Stage stage = await SeedStageAsync(db, division, StageType.Group, "Grupo A");
        List<Team> teams = await SeedTeamsAsync(db, tournament, 2);
        await SeedRegistrationAsync(db, division, teams[0]);
        await SeedRegistrationAsync(db, division, teams[1]);
        await SeedStageTeamMatchAsync(db, stage, teams[0]);

        List<Team> roster = await rosterService.GetRosterAsync(division.Id);

        Assert.Equal(2, roster.Count);
        Assert.Contains(roster, t => t.Id == teams[0].Id);
        Assert.Contains(roster, t => t.Id == teams[1].Id);
    }

    private static async Task<Tournament> SeedTournamentAsync(
        ApplicationDBContext db, TournamentStatus status = TournamentStatus.Scheduled)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Roster service test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = startDate.AddDays(-1),
            StartDate = startDate,
            Status = status,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        return tournament;
    }

    private static async Task<Division> SeedDivisionAsync(
        ApplicationDBContext db, Tournament tournament, bool isCrossDivisionCup = false)
    {
        Division division = new()
        {
            Slug = $"division-{Guid.NewGuid()}",
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

    private static async Task<Stage> SeedStageAsync(
        ApplicationDBContext db, Division division, StageType stageType, string name)
    {
        Stage stage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"{name}-{Guid.NewGuid()}",
            StageType = stageType,
            IsActive = true,
            StartDate = division.Tournament!.StartDate,
            EndDate = division.Tournament!.StartDate.AddDays(30),
            DivisionId = division.Id,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        db.Stages.Add(stage);
        await db.SaveChangesAsync();

        return stage;
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
                LogoUrl = "https://example.test/logo.png",
                ShirtColor = "Blue",
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

    private static async Task SeedRegistrationAsync(ApplicationDBContext db, Division division, Team team)
    {
        db.DivisionTeamRegistrations.Add(new DivisionTeamRegistration
        {
            TeamId = team.Id,
            DivisionId = division.Id,
            CreatedBy = "test",
        });

        await db.SaveChangesAsync();
    }

    private static async Task SeedStageTeamMatchAsync(ApplicationDBContext db, Stage stage, Team team)
    {
        db.StageTeamMatches.Add(new StageTeamMatch
        {
            StageId = stage.Id,
            TeamId = team.Id,
            CreatedBy = "test",
        });

        await db.SaveChangesAsync();
    }
}
