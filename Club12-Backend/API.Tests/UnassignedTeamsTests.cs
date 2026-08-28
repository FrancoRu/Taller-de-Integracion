using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Verifies IDivisionService.GetUnassignedTeamsAsync surfaces registered
/// teams that don't yet belong to any division — the "a team must be in a
/// division if it's part of the tournament" completeness rule, checked as
/// a readiness signal (not a hard block) so an admin can build the
/// tournament up incrementally.
/// </summary>
public class UnassignedTeamsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public UnassignedTeamsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetUnassignedTeamsAsync_TeamNeverAssignedToAnyStage_IsReturned()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionService divisionService = scope.ServiceProvider.GetRequiredService<IDivisionService>();

        Tournament tournament = await SeedTournamentAsync(db);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 2);
        Division zoneA = await SeedDivisionAsync(db, tournament);
        Stage zoneAStage = await SeedStageAsync(db, zoneA, tournament);
        await AssignTeamAsync(db, zoneAStage, teams[0]);

        List<Team> unassigned = await divisionService.GetUnassignedTeamsAsync(tournament.Id);

        Team onlyResult = Assert.Single(unassigned);
        Assert.Equal(teams[1].Id, onlyResult.Id);
    }

    [Fact]
    public async Task GetUnassignedTeamsAsync_EveryTeamAssigned_ReturnsEmpty()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionService divisionService = scope.ServiceProvider.GetRequiredService<IDivisionService>();

        Tournament tournament = await SeedTournamentAsync(db);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 2);
        Division zoneA = await SeedDivisionAsync(db, tournament);
        Stage zoneAStage = await SeedStageAsync(db, zoneA, tournament);
        await AssignTeamAsync(db, zoneAStage, teams[0]);
        await AssignTeamAsync(db, zoneAStage, teams[1]);

        List<Team> unassigned = await divisionService.GetUnassignedTeamsAsync(tournament.Id);

        Assert.Empty(unassigned);
    }

    [Fact]
    public async Task GetUnassignedTeamsAsync_TeamOnlyInCrossDivisionCup_StillCountsAsAssigned()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionService divisionService = scope.ServiceProvider.GetRequiredService<IDivisionService>();

        Tournament tournament = await SeedTournamentAsync(db);
        List<Team> teams = await SeedTeamsAsync(db, tournament, 1);
        Division cup = await SeedDivisionAsync(db, tournament, isCrossDivisionCup: true);
        Stage cupStage = await SeedStageAsync(db, cup, tournament);
        await AssignTeamAsync(db, cupStage, teams[0]);

        List<Team> unassigned = await divisionService.GetUnassignedTeamsAsync(tournament.Id);

        Assert.Empty(unassigned);
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Unassigned-teams test tournament",
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

    private static async Task<Division> SeedDivisionAsync(ApplicationDBContext db, Tournament tournament, bool isCrossDivisionCup = false)
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

    private static async Task<Stage> SeedStageAsync(ApplicationDBContext db, Division division, Tournament tournament)
    {
        Stage stage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
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

    private static async Task AssignTeamAsync(ApplicationDBContext db, Stage stage, Team team)
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
