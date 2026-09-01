using Application.DTOs.Divisions.Request;
using Application.DTOs.Tournament.Request;
using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using System.Net;
using System.Net.Http.Json;

namespace API.Tests;

/// <summary>
/// HU-31/HU-112: POST api/tournaments/{tournamentId}/divisions/full adds ONE
/// division (with its group stage, cups and playoff mappings) to an
/// already-existing tournament in a single transaction — the same structure
/// guarantee a wizard-created division gets, instead of the bare division the
/// granular POST api/divisions endpoint leaves behind (no stages, no cups).
/// </summary>
public class TournamentAddFullDivisionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TournamentAddFullDivisionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static CreateFullStageRequest GroupStage() => new()
    {
        Name = "Fase de Grupos",
        StageType = StageType.Group,
        StartDate = DateTime.UtcNow.Date.AddDays(30),
        EndDate = DateTime.UtcNow.Date.AddDays(51),
        RoundRobinLegs = 1,
    };

    private static CreateFullDivisionRequest BuildDivisionRequest(TournamentCategory category) => new()
    {
        Name = $"Zona {Guid.NewGuid():N}",
        Category = category,
        PointsForWin = 2,
        PointsForLoss = 1,
        PlayoffMappings =
        [
            new PlayoffMappingRequest { FromPosition = 1, ToPosition = 4, Destination = "Copa Oro" },
        ],
        Stages = [GroupStage()],
    };

    [Fact]
    public async Task AddFullDivisionAsync_PersistsDivisionWithStagesAndPlayoffMappingsAtomically()
    {
        Guid tournamentId;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.OpenForRegistration);
            tournamentId = tournament.Id;
        }

        Guid createdDivisionId;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ITournamentService tournamentService =
                scope.ServiceProvider.GetRequiredService<ITournamentService>();
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

            Tournament tournament = await db.Tournaments.SingleAsync(t => t.Id == tournamentId);
            Division created = await tournamentService.AddFullDivisionAsync(
                tournament, BuildDivisionRequest(TournamentCategory.Masculine));

            createdDivisionId = created.Id;
        }

        // Verify in a FRESH scope/context that the whole graph is on disk.
        using IServiceScope verify = _factory.Services.CreateScope();
        ApplicationDBContext verifyDb = verify.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Division division = await verifyDb.Divisions
            .Include(d => d.PlayoffMappings)
            .SingleAsync(d => d.Id == createdDivisionId);

        Assert.Equal(tournamentId, division.TournamentId);
        Assert.Single(division.PlayoffMappings);
        Assert.Equal(1, await verifyDb.Stages.CountAsync(s => s.DivisionId == createdDivisionId));
    }

    /// <summary>
    /// Structural guard (HU-31): a division cannot be added once registration
    /// closes. Mapped to 409 by the global handler (InvalidOperationException).
    /// </summary>
    [Fact]
    public async Task AddFullDivision_TournamentNotOpenForRegistration_IsRejected()
    {
        Guid tournamentId;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.RegistrationClosed);
            tournamentId = tournament.Id;
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/tournaments/{tournamentId}/divisions/full",
            BuildDivisionRequest(TournamentCategory.Masculine));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using IServiceScope verify = _factory.Services.CreateScope();
        ApplicationDBContext verifyDb = verify.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Assert.Equal(0, await verifyDb.Divisions.AsNoTracking().CountAsync(d => d.TournamentId == tournamentId));
    }

    /// <summary>
    /// HU-48: a division's category must match its tournament's — mapped to
    /// 409, and nothing (not even the division shell) is left behind.
    /// </summary>
    [Fact]
    public async Task AddFullDivision_CategoryMismatch_IsRejectedAndPersistsNothing()
    {
        Guid tournamentId;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            Tournament tournament = await SeedTournamentAsync(db, TournamentStatus.OpenForRegistration);
            tournamentId = tournament.Id;
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/tournaments/{tournamentId}/divisions/full",
            BuildDivisionRequest(TournamentCategory.Feminine));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using IServiceScope verify = _factory.Services.CreateScope();
        ApplicationDBContext verifyDb = verify.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        Assert.Equal(0, await verifyDb.Divisions.AsNoTracking().CountAsync(d => d.TournamentId == tournamentId));
    }

    [Fact]
    public async Task AddFullDivision_TournamentNotFound_ReturnsNotFound()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/tournaments/{Guid.NewGuid()}/divisions/full",
            BuildDivisionRequest(TournamentCategory.Masculine));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db, TournamentStatus status)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Add-full-division test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = startDate.AddDays(-1),
            StartDate = startDate,
            Category = TournamentCategory.Masculine,
            Status = status,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        return tournament;
    }
}
