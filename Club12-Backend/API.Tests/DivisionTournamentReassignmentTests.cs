using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Verifies IDivisionService.TryAssignTournamentAsync — the capability that
/// lets a division (and everything under it: stages, matches, and team
/// assignments, none of which carry their own tournament reference) be
/// moved to a different tournament after creation. Exercised through the
/// same two-step flow the controller uses: mutate in memory via
/// TryAssignTournamentAsync, then persist via UpdateDivisionAsync.
/// </summary>
public class DivisionTournamentReassignmentTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DivisionTournamentReassignmentTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TryAssignTournamentAsync_TargetTournamentExists_MovesDivisionAndItsStages()
    {
        using IServiceScope seedScope = _factory.Services.CreateScope();
        ApplicationDBContext seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Tournament sourceTournament = await SeedTournamentAsync(seedDb);
        Tournament targetTournament = await SeedTournamentAsync(seedDb);
        Division division = await SeedDivisionAsync(seedDb, sourceTournament);
        Stage stage = await SeedStageAsync(seedDb, division, sourceTournament);

        using (IServiceScope actScope = _factory.Services.CreateScope())
        {
            IDivisionService divisionService = actScope.ServiceProvider.GetRequiredService<IDivisionService>();
            ApplicationDBContext actDb = actScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            Division tracked = await actDb.Divisions.SingleAsync(d => d.Id == division.Id);

            bool reassigned = await divisionService.TryAssignTournamentAsync(tracked, targetTournament.Id);
            Assert.True(reassigned);

            await divisionService.UpdateDivisionAsync(tracked);
        }

        using IServiceScope verifyScope = _factory.Services.CreateScope();
        ApplicationDBContext verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Division? persisted = await verifyDb.Divisions
            .AsNoTracking()
            .Include(d => d.Stages)
            .SingleOrDefaultAsync(d => d.Id == division.Id);

        Assert.NotNull(persisted);
        Assert.Equal(targetTournament.Id, persisted!.TournamentId);
        Assert.Contains(persisted.Stages, s => s.Id == stage.Id);
    }

    [Fact]
    public async Task TryAssignTournamentAsync_SameTournament_IsNoopAndReturnsTrue()
    {
        using IServiceScope seedScope = _factory.Services.CreateScope();
        ApplicationDBContext seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Tournament tournament = await SeedTournamentAsync(seedDb);
        Division division = await SeedDivisionAsync(seedDb, tournament);

        using IServiceScope actScope = _factory.Services.CreateScope();
        IDivisionService divisionService = actScope.ServiceProvider.GetRequiredService<IDivisionService>();
        ApplicationDBContext actDb = actScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Division tracked = await actDb.Divisions.SingleAsync(d => d.Id == division.Id);

        bool reassigned = await divisionService.TryAssignTournamentAsync(tracked, tournament.Id);

        Assert.True(reassigned);
        Assert.Equal(tournament.Id, tracked.TournamentId);
    }

    [Fact]
    public async Task TryAssignTournamentAsync_TargetTournamentDoesNotExist_ReturnsFalseAndLeavesDivisionUnchanged()
    {
        using IServiceScope seedScope = _factory.Services.CreateScope();
        ApplicationDBContext seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Tournament sourceTournament = await SeedTournamentAsync(seedDb);
        Division division = await SeedDivisionAsync(seedDb, sourceTournament);

        using IServiceScope actScope = _factory.Services.CreateScope();
        IDivisionService divisionService = actScope.ServiceProvider.GetRequiredService<IDivisionService>();
        ApplicationDBContext actDb = actScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Division tracked = await actDb.Divisions.SingleAsync(d => d.Id == division.Id);

        bool reassigned = await divisionService.TryAssignTournamentAsync(tracked, Guid.NewGuid());

        Assert.False(reassigned);
        Assert.Equal(sourceTournament.Id, tracked.TournamentId);
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Division-reassignment test tournament",
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

    private static async Task<Division> SeedDivisionAsync(ApplicationDBContext db, Tournament tournament)
    {
        Division division = new()
        {
            Slug = $"division-{Guid.NewGuid()}",
            Name = $"Division-{Guid.NewGuid()}",
            Tournament = tournament,
            TournamentId = tournament.Id,
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
}
