using Application.DTOs.Divisions.Request;
using Application.DTOs.Tournament.Request;
using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// HU-38: the compound POST /api/tournaments/full path
/// (TournamentService.CreateFullTournamentAsync) persists a tournament and its
/// whole division/stage graph in ONE transaction. On any failure the whole
/// thing rolls back, so no partial tournament rows survive.
/// </summary>
public class FullTournamentCreationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public FullTournamentCreationTests(CustomWebApplicationFactory factory)
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

    private static CreateFullTournamentRequest BuildRequest(
        string name, TournamentCategory tournamentCategory, TournamentCategory secondDivisionCategory)
    {
        DateTime start = DateTime.UtcNow.Date.AddDays(30);

        return new CreateFullTournamentRequest
        {
            Name = name,
            Description = "HU-38 compound create",
            StartDate = start,
            TeamRegistrationDeadline = start.AddDays(-1),
            Category = tournamentCategory,
            Divisions =
            [
                new CreateFullDivisionRequest
                {
                    Name = $"{name} - Zona A",
                    Category = tournamentCategory,
                    PointsForWin = 3,
                    PointsForLoss = 0,
                    PlayoffMappings =
                    [
                        new PlayoffMappingRequest { FromPosition = 1, ToPosition = 4, Destination = "Copa Oro" },
                    ],
                    Stages = [GroupStage()],
                },
                new CreateFullDivisionRequest
                {
                    Name = $"{name} - Zona B",
                    Category = secondDivisionCategory,
                    Stages = [GroupStage()],
                },
            ],
        };
    }

    [Fact]
    public async Task CreateFullTournamentAsync_PersistsTournamentWithDivisionsAndStagesAtomically()
    {
        string name = $"Full-OK-{Guid.NewGuid():N}";

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ITournamentService tournamentService =
                scope.ServiceProvider.GetRequiredService<ITournamentService>();

            Tournament created = await tournamentService.CreateFullTournamentAsync(
                BuildRequest(name, TournamentCategory.Masculine, TournamentCategory.Masculine));

            Assert.NotEqual(Guid.Empty, created.Id);
            Assert.Equal(TournamentStatus.OpenForRegistration, created.Status);
        }

        // Verify in a FRESH scope/context that the whole graph is on disk.
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

            Tournament tournament = await db.Tournaments
                .Include(t => t.Divisions)
                .SingleAsync(t => t.Name == name);

            Assert.Equal(2, tournament.Divisions.Count);

            foreach (Division division in tournament.Divisions)
            {
                Assert.Equal(tournament.Id, division.TournamentId);

                int stageCount = await db.Stages.CountAsync(s => s.DivisionId == division.Id);
                Assert.Equal(1, stageCount);
            }

            // The first division's playoff mapping was persisted too.
            List<Guid> divisionIds = tournament.Divisions.Select(d => d.Id).ToList();
            int mappingCount = await db.DivisionPlayoffMappings.CountAsync(m => divisionIds.Contains(m.DivisionId));
            Assert.Equal(1, mappingCount);
        }
    }

    [Fact]
    public async Task CreateFullTournamentAsync_RollsBackFully_WhenADivisionIsInvalid()
    {
        string name = $"Full-Fail-{Guid.NewGuid():N}";

        int tournamentsBefore;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            tournamentsBefore = await db.Tournaments.CountAsync();
        }

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ITournamentService tournamentService =
                scope.ServiceProvider.GetRequiredService<ITournamentService>();

            // Second division's category mismatches the tournament (HU-48), which
            // throws mid-way through the transaction — after the tournament and
            // first division were already inserted.
            CreateFullTournamentRequest request =
                BuildRequest(name, TournamentCategory.Masculine, TournamentCategory.Feminine);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => tournamentService.CreateFullTournamentAsync(request));
        }

        // Nothing partial survives: no tournament, no divisions, no stages.
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

            Assert.False(await db.Tournaments.AnyAsync(t => t.Name == name));
            Assert.Equal(tournamentsBefore, await db.Tournaments.CountAsync());
            Assert.False(await db.Divisions.AnyAsync(d => d.Name.StartsWith(name)));
        }
    }
}
