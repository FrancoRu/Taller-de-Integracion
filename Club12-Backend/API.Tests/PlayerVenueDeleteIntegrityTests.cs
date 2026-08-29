using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Integrity tests for player and venue deletion: a player with match
/// statistics, scorer records or sanctions cannot be deleted (it would orphan
/// tournament history), and a venue referenced by matches cannot be deleted.
/// Players/venues with no such references are removed cleanly (a player's
/// season registrations are cleaned up too). Exercised through the real
/// services/repositories against the SQLite-backed integration host.
/// </summary>
public class PlayerVenueDeleteIntegrityTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PlayerVenueDeleteIntegrityTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---------- Player ----------

    [Fact]
    public async Task DeletePlayer_WithStatistics_IsBlocked()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerService playerService = scope.ServiceProvider.GetRequiredService<IPlayerService>();

        (Team team, Stage stage, Guid tournamentId) = await SeedTeamAndStageAsync(db);
        Player player = await SeedPlayerAsync(db, team, tournamentId);
        Match match = await SeedMatchAsync(db, stage, venueId: null);

        db.Set<PlayerStatistic>().Add(new PlayerStatistic
        {
            Value = 12,
            Type = StatisticType.Points,
            MatchId = match.Id,
            PlayerId = player.Id,
            CreatedBy = "test",
        });
        await db.SaveChangesAsync();

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => playerService.DeletePlayerAsync(player.Id));

        Assert.Contains("No se puede eliminar", ex.Message);
        Assert.True(await db.Players.AnyAsync(p => p.Id == player.Id));
    }

    [Fact]
    public async Task DeletePlayer_WithSanction_IsBlocked()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerService playerService = scope.ServiceProvider.GetRequiredService<IPlayerService>();

        (Team team, Stage stage, Guid tournamentId) = await SeedTeamAndStageAsync(db);
        Player player = await SeedPlayerAsync(db, team, tournamentId);
        Match match = await SeedMatchAsync(db, stage, venueId: null);

        db.Set<PlayerSanction>().Add(new PlayerSanction
        {
            Duration = 1,
            IssuedDate = DateTime.UtcNow,
            Description = "Test sanction",
            Slug = $"sanction-{Guid.NewGuid()}",
            SubjectType = SanctionSubjectType.Player,
            PlayerId = player.Id,
            Match = match,
            MatchId = match.Id,
            CreatedBy = "test",
        });
        await db.SaveChangesAsync();

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => playerService.DeletePlayerAsync(player.Id));

        Assert.Contains("No se puede eliminar", ex.Message);
        Assert.True(await db.Players.AnyAsync(p => p.Id == player.Id));
    }

    [Fact]
    public async Task DeletePlayer_WithoutHistory_RemovesPlayerAndRegistrations()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerService playerService = scope.ServiceProvider.GetRequiredService<IPlayerService>();

        (Team team, _, Guid tournamentId) = await SeedTeamAndStageAsync(db);
        Player player = await SeedPlayerAsync(db, team, tournamentId);

        Assert.True(await db.PlayerTeamRegistrations.AnyAsync(r => r.PlayerId == player.Id));

        await playerService.DeletePlayerAsync(player.Id);

        Assert.False(await db.Players.AnyAsync(p => p.Id == player.Id));
        Assert.False(await db.PlayerTeamRegistrations.AnyAsync(r => r.PlayerId == player.Id));
    }

    // ---------- Venue ----------

    [Fact]
    public async Task DeleteVenue_ReferencedByMatch_IsBlocked()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IVenueService venueService = scope.ServiceProvider.GetRequiredService<IVenueService>();

        (_, Stage stage, _) = await SeedTeamAndStageAsync(db);
        Venue venue = await SeedVenueAsync(db);
        await SeedMatchAsync(db, stage, venueId: venue.Id);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => venueService.DeleteVenueAsync(venue.Id));

        Assert.Contains("No se puede eliminar", ex.Message);
        Assert.True(await db.Venues.AnyAsync(v => v.Id == venue.Id));
    }

    [Fact]
    public async Task DeleteVenue_NotReferenced_RemovesVenue()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IVenueService venueService = scope.ServiceProvider.GetRequiredService<IVenueService>();

        Venue venue = await SeedVenueAsync(db);

        await venueService.DeleteVenueAsync(venue.Id);

        Assert.False(await db.Venues.AnyAsync(v => v.Id == venue.Id));
    }

    // ---------- seeding ----------

    private static async Task<(Team Team, Stage Stage, Guid TournamentId)> SeedTeamAndStageAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = "Delete-integrity test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = startDate.AddDays(-1),
            StartDate = startDate,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        Team team = new()
        {
            Name = $"Team-{Guid.NewGuid()}",
            Slug = $"team-{Guid.NewGuid()}",
            ThreeLetterCode = Guid.NewGuid().ToString("N")[..3].ToUpperInvariant(),
            LogoUrl = "https://example.test/logo.png",
            ShirtColor = "Blue",
            Tournament = tournament,
            TournamentId = tournament.Id,
            Players = [],
            CreatedBy = "test",
        };

        Division division = new()
        {
            Slug = $"division-{Guid.NewGuid()}",
            Name = $"Division-{Guid.NewGuid()}",
            Tournament = tournament,
            TournamentId = tournament.Id,
            Stages = [],
            CreatedBy = "test",
        };

        db.Tournaments.Add(tournament);
        db.Teams.Add(team);
        db.Divisions.Add(division);
        await db.SaveChangesAsync();

        Stage stage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Name = $"Group-{Guid.NewGuid()}",
            StageType = StageType.Group,
            IsActive = true,
            IsElimination = false,
            StartDate = startDate,
            EndDate = startDate.AddDays(7),
            Division = division,
            DivisionId = division.Id,
            Matches = [],
            CreatedBy = "test",
        };

        db.Stages.Add(stage);
        await db.SaveChangesAsync();

        return (team, stage, tournament.Id);
    }

    private static async Task<Player> SeedPlayerAsync(ApplicationDBContext db, Team team, Guid tournamentId)
    {
        Player player = new()
        {
            Slug = $"player-{Guid.NewGuid()}",
            FirstName = "Test",
            LastName = $"Player-{Guid.NewGuid():N}",
            SecondName = null,
            DocumentNumber = Guid.NewGuid().ToString("N")[..10],
            IsSanctioned = false,
            BirthDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SocialSecurity = "OSDE",
            Team = team,
            TeamId = team.Id,
            CreatedBy = "test",
        };

        db.Players.Add(player);
        db.PlayerTeamRegistrations.Add(new PlayerTeamRegistration
        {
            Player = player,
            PlayerId = player.Id,
            TeamId = team.Id,
            TournamentId = tournamentId,
            CreatedBy = "test",
        });
        await db.SaveChangesAsync();

        return player;
    }

    private static async Task<Match> SeedMatchAsync(ApplicationDBContext db, Stage stage, Guid? venueId)
    {
        Match match = new()
        {
            MatchDate = stage.StartDate,
            Type = MatchType.Regular,
            Slug = $"match-{Guid.NewGuid()}",
            IsFinished = true,
            Status = MatchStatus.Played,
            Stage = stage,
            StageId = stage.Id,
            VenueId = venueId,
            CreatedBy = "test",
        };

        db.Matches.Add(match);
        await db.SaveChangesAsync();

        return match;
    }

    private static async Task<Venue> SeedVenueAsync(ApplicationDBContext db)
    {
        Venue venue = new()
        {
            Name = $"Venue-{Guid.NewGuid()}",
            Slug = $"venue-{Guid.NewGuid()}",
            Address = "123 Test St",
            CreatedBy = "test",
        };

        db.Venues.Add(venue);
        await db.SaveChangesAsync();

        return venue;
    }
}
