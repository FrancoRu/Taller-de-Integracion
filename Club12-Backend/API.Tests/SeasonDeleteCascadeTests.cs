using Application.Interfaces.Services;

using Domain.Constants;
using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Season→Tournament is DB-level SetNull, not Cascade (a season is a purely
/// additive grouping and must never silently wipe a tournament's history —
/// see <c>SeasonEntityConfiguration</c>). A bare bulk delete of the Season
/// row would therefore only detach its tournaments, leaving them alive but
/// orphaned: no season, yet still enrolling teams, still blocking those
/// teams from being deleted, and invisible from any season-scoped screen.
/// <see cref="ISeasonService.DeleteSeasonAsync"/> must route through
/// <see cref="ITournamentService.DeleteTournamentAsync"/> for each of the
/// season's tournaments instead.
/// </summary>
public class SeasonDeleteCascadeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SeasonDeleteCascadeTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task<(Guid SeasonId, Guid TournamentId)> CreateSeasonWithTournamentAsync(
        IServiceProvider services, string label)
    {
        ISeasonService seasonService = services.GetRequiredService<ISeasonService>();
        ITournamentService tournamentService = services.GetRequiredService<ITournamentService>();

        Season season = await seasonService.CreateSeasonAsync(new Season
        {
            Name = $"Temp-{label}-{Guid.NewGuid():N}",
            Slug = null!,
            Year = 2026,
            CreatedBy = AuditConstants.SystemUser,
        });

        Tournament tournament = new()
        {
            Name = $"Torneo-{label}-{Guid.NewGuid():N}",
            Slug = null!,
            Description = "cascade-delete test tournament",
            StartDate = DateTime.UtcNow.Date.AddDays(30),
            TeamRegistrationDeadline = DateTime.UtcNow.Date.AddDays(29),
            Category = TournamentCategory.Masculine,
            SeasonId = season.Id,
            CreatedBy = AuditConstants.SystemUser,
            Divisions = [],
            Teams = [],
        };
        await tournamentService.CreateTournamentAsync(tournament);

        return (season.Id, tournament.Id);
    }

    [Fact]
    public async Task DeleteSeason_DeletesItsTournament_InsteadOfOrphaningIt()
    {
        Guid seasonId, tournamentId;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            (seasonId, tournamentId) =
                await CreateSeasonWithTournamentAsync(scope.ServiceProvider, "Cascade");
        }

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ISeasonService seasonService = scope.ServiceProvider.GetRequiredService<ISeasonService>();
            await seasonService.DeleteSeasonAsync(seasonId);
        }

        using IServiceScope assertScope = _factory.Services.CreateScope();
        ApplicationDBContext db = assertScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Assert.Null(await db.Seasons.SingleOrDefaultAsync(s => s.Id == seasonId));
        // The tournament must be gone entirely, not merely orphaned with a
        // null SeasonId — an orphan would still block its enrolled teams
        // from being deleted and would be unreachable from the UI.
        Assert.Null(await db.Tournaments.SingleOrDefaultAsync(t => t.Id == tournamentId));
    }

    [Fact]
    public async Task DeleteSeason_WithAnOngoingTournament_ThrowsAndKeepsBoth()
    {
        Guid seasonId, tournamentId;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            (seasonId, tournamentId) =
                await CreateSeasonWithTournamentAsync(scope.ServiceProvider, "Blocked");

            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            Tournament tournament = await db.Tournaments.SingleAsync(t => t.Id == tournamentId);
            tournament.Status = TournamentStatus.Ongoing;
            await db.SaveChangesAsync();
        }

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ISeasonService seasonService = scope.ServiceProvider.GetRequiredService<ISeasonService>();
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => seasonService.DeleteSeasonAsync(seasonId));
        }

        using IServiceScope assertScope = _factory.Services.CreateScope();
        ApplicationDBContext db2 = assertScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        // The failed delete must not leave the season gone with the
        // tournament orphaned — both must still exist, linked, so the admin
        // sees exactly why the season can't be deleted.
        Assert.NotNull(await db2.Seasons.SingleOrDefaultAsync(s => s.Id == seasonId));
        Tournament survivingTournament =
            await db2.Tournaments.SingleAsync(t => t.Id == tournamentId);
        Assert.Equal(seasonId, survivingTournament.SeasonId);
    }
}
