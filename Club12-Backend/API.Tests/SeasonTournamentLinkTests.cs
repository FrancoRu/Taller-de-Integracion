using Application.DTOs.Tournament.Request;
using Application.Interfaces.Services;

using AutoMapper;

using Domain.Constants;
using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// A tournament created under a season must actually persist its
/// <see cref="Tournament.SeasonId"/>, so the season's Tournaments collection
/// (what the admin "Temporadas" grid counts) reflects it. Covers BOTH create
/// paths: the incremental base create the wizard uses (POST /api/tournaments,
/// mapper + CreateTournamentAsync) and the compound create
/// (POST /api/tournaments/full, CreateFullTournamentAsync).
/// </summary>
public class SeasonTournamentLinkTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SeasonTournamentLinkTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task<Guid> CreateSeasonAsync(IServiceProvider services, string name)
    {
        ISeasonService seasonService = services.GetRequiredService<ISeasonService>();
        Season season = await seasonService.CreateSeasonAsync(new Season
        {
            Name = name,
            Slug = null!,
            Year = 2026,
            CreatedBy = AuditConstants.SystemUser,
        });
        return season.Id;
    }

    [Fact]
    public async Task BaseCreate_LinksTournamentToItsSeason()
    {
        string seasonName = $"Temp-Base-{Guid.NewGuid():N}";
        string tournamentName = $"Torneo-Base-{Guid.NewGuid():N}";
        Guid seasonId;

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            seasonId = await CreateSeasonAsync(scope.ServiceProvider, seasonName);

            IMapper mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
            ITournamentService tournamentService =
                scope.ServiceProvider.GetRequiredService<ITournamentService>();

            // Exactly what TournamentController.CreateTournament does: bind the
            // wizard's payload (which carries seasonId) and map it to the entity.
            CreateTournamentRequest request = new()
            {
                Name = tournamentName,
                Description = "wizard base create",
                StartDate = DateTime.UtcNow.Date.AddDays(30),
                TeamRegistrationDeadline = DateTime.UtcNow.Date.AddDays(29),
                Category = TournamentCategory.Masculine,
                SeasonId = seasonId,
            };

            Tournament tournament = mapper.Map<Tournament>(request);
            await tournamentService.CreateTournamentAsync(tournament);
        }

        await AssertSeasonHasOneTournamentAsync(seasonId, tournamentName);
    }

    [Fact]
    public async Task FullCreate_LinksTournamentToItsSeason()
    {
        string seasonName = $"Temp-Full-{Guid.NewGuid():N}";
        string tournamentName = $"Torneo-Full-{Guid.NewGuid():N}";
        Guid seasonId;

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            seasonId = await CreateSeasonAsync(scope.ServiceProvider, seasonName);

            ITournamentService tournamentService =
                scope.ServiceProvider.GetRequiredService<ITournamentService>();

            CreateFullTournamentRequest request = new()
            {
                Name = tournamentName,
                Description = "wizard full create",
                StartDate = DateTime.UtcNow.Date.AddDays(30),
                TeamRegistrationDeadline = DateTime.UtcNow.Date.AddDays(29),
                Category = TournamentCategory.Masculine,
                SeasonId = seasonId,
                Divisions = [],
            };

            await tournamentService.CreateFullTournamentAsync(request);
        }

        await AssertSeasonHasOneTournamentAsync(seasonId, tournamentName);
    }

    private async Task AssertSeasonHasOneTournamentAsync(Guid seasonId, string tournamentName)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Tournament tournament = await db.Tournaments.SingleAsync(t => t.Name == tournamentName);
        Assert.Equal(seasonId, tournament.SeasonId);

        // This is exactly what the admin "Temporadas" grid counts.
        Season season = await db.Seasons
            .Include(s => s.Tournaments)
            .SingleAsync(s => s.Id == seasonId);
        Assert.Contains(season.Tournaments, t => t.Name == tournamentName);
    }
}
