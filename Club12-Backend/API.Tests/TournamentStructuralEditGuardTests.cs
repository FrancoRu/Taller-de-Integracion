using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// HU-31: structural changes (creating/editing divisions, adding/removing team
/// registrations) are only permitted while a tournament is
/// <see cref="TournamentStatus.OpenForRegistration"/>. Every other status
/// freezes the structure at the service layer.
/// </summary>
public class TournamentStructuralEditGuardTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TournamentStructuralEditGuardTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(TournamentStatus.Scheduled)]
    [InlineData(TournamentStatus.RegistrationClosed)]
    [InlineData(TournamentStatus.Ongoing)]
    [InlineData(TournamentStatus.Finished)]
    [InlineData(TournamentStatus.Canceled)]
    public async Task CreateDivisionAsync_TournamentNotOpenForRegistration_Throws(TournamentStatus status)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionService divisionService = scope.ServiceProvider.GetRequiredService<IDivisionService>();

        Tournament tournament = await SeedTournamentAsync(db, status);

        Division division = new()
        {
            Name = $"Division-{Guid.NewGuid()}",
            Slug = string.Empty,
            Tournament = tournament,
            TournamentId = tournament.Id,
            Stages = [],
            CreatedBy = "test",
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => divisionService.CreateDivisionAsync(division));

        Assert.Equal(0, await db.Divisions.CountAsync(d => d.TournamentId == tournament.Id));
    }

    [Fact]
    public async Task CreateDivisionAsync_TournamentOpenForRegistration_Succeeds()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionService divisionService = scope.ServiceProvider.GetRequiredService<IDivisionService>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.OpenForRegistration);

        Division division = new()
        {
            Name = $"Division-{Guid.NewGuid()}",
            Slug = string.Empty,
            Tournament = tournament,
            TournamentId = tournament.Id,
            Stages = [],
            CreatedBy = "test",
        };

        Division created = await divisionService.CreateDivisionAsync(division);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(1, await db.Divisions.CountAsync(d => d.TournamentId == tournament.Id));
    }

    [Fact]
    public async Task UpdateDivisionAsync_TournamentNotOpenForRegistration_Throws()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IDivisionService divisionService = scope.ServiceProvider.GetRequiredService<IDivisionService>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.RegistrationClosed);
        Division division = new()
        {
            Name = $"Division-{Guid.NewGuid()}",
            Slug = $"division-{Guid.NewGuid()}",
            Tournament = tournament,
            TournamentId = tournament.Id,
            Stages = [],
            CreatedBy = "test",
        };
        db.Divisions.Add(division);
        await db.SaveChangesAsync();

        division.Name = "Renamed";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => divisionService.UpdateDivisionAsync(division));
    }

    [Theory]
    [InlineData(TournamentStatus.Scheduled)]
    [InlineData(TournamentStatus.RegistrationClosed)]
    [InlineData(TournamentStatus.Ongoing)]
    [InlineData(TournamentStatus.Finished)]
    [InlineData(TournamentStatus.Canceled)]
    public async Task RegisterTeamsToTournamentAsync_TournamentNotOpenForRegistration_Throws(TournamentStatus status)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITeamService teamService = scope.ServiceProvider.GetRequiredService<ITeamService>();

        Tournament tournament = await SeedTournamentAsync(db, status);
        Team team = await SeedTeamAsync(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => teamService.RegisterTeamsToTournamentAsync(tournament, [team.Id]));

        Assert.Equal(0, await db.TeamTournamentRegistrations.CountAsync(r => r.TournamentId == tournament.Id));
    }

    [Fact]
    public async Task RegisterTeamsToTournamentAsync_TournamentOpenForRegistration_Succeeds()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        ITeamService teamService = scope.ServiceProvider.GetRequiredService<ITeamService>();

        Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.OpenForRegistration);
        Team team = await SeedTeamAsync(db);

        await teamService.RegisterTeamsToTournamentAsync(tournament, [team.Id]);

        Assert.Equal(1, await db.TeamTournamentRegistrations.CountAsync(r => r.TournamentId == tournament.Id));
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db, TournamentStatus status)
    {
        DateTime start = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Structural guard test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = start.AddDays(-1),
            StartDate = start,
            Status = status,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        return tournament;
    }

    private static async Task<Team> SeedTeamAsync(ApplicationDBContext db)
    {
        Team team = new()
        {
            Name = $"Team-{Guid.NewGuid()}",
            Slug = $"team-{Guid.NewGuid()}",
            ThreeLetterCode = "TMX",
            LogoUrl = "https://example.com/logo.png",
            ShirtColor = "Red",
            Players = [],
            CreatedBy = "test",
        };

        db.Teams.Add(team);
        await db.SaveChangesAsync();

        return team;
    }
}
