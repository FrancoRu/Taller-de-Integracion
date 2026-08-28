using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Services;
using Application.Utils.Options;

using Domain.Entities.Models;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Xunit;

namespace API.Tests;

/// <summary>
/// HU-54: roster limits and unique dorsals. The dorsal lives on the
/// season-scoped PlayerTeamRegistration, so all three invariants
/// (no two teams per tournament, roster-size cap, unique dorsal within a
/// team+tournament) are enforced by PlayerService.RegisterPlayerToTeamAsync.
/// </summary>
public class RosterRegistrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RosterRegistrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RegisterPlayer_DuplicateDorsalInSameTeamAndTournament_IsRejected()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerService service = scope.ServiceProvider.GetRequiredService<IPlayerService>();

        (Guid tournamentId, Guid teamId, _, List<Guid> players) = await SeedAsync(db, playerCount: 2);

        await service.RegisterPlayerToTeamAsync(players[0], teamId, tournamentId, jerseyNumber: 10);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RegisterPlayerToTeamAsync(players[1], teamId, tournamentId, jerseyNumber: 10));

        Assert.Contains("Jersey number", ex.Message);
    }

    [Fact]
    public async Task RegisterPlayer_ExceedingRosterSizeCap_IsRejected()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        (Guid tournamentId, Guid teamId, _, List<Guid> players) = await SeedAsync(db, playerCount: 3);

        // Build a PlayerService with a deliberately tiny cap so the test is
        // deterministic without seeding dozens of players.
        PlayerService service = new(unitOfWork, Options.Create(new RosterOptions { MaxPlayersPerTeam = 2 }));

        await service.RegisterPlayerToTeamAsync(players[0], teamId, tournamentId, jerseyNumber: 1);
        await service.RegisterPlayerToTeamAsync(players[1], teamId, tournamentId, jerseyNumber: 2);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RegisterPlayerToTeamAsync(players[2], teamId, tournamentId, jerseyNumber: 3));

        Assert.Contains("maximum", ex.Message);
    }

    [Fact]
    public async Task RegisterPlayer_ToTwoTeamsInSameTournament_IsRejected()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerService service = scope.ServiceProvider.GetRequiredService<IPlayerService>();

        (Guid tournamentId, Guid teamAId, Guid teamBId, List<Guid> players) = await SeedAsync(db, playerCount: 1);

        await service.RegisterPlayerToTeamAsync(players[0], teamAId, tournamentId, jerseyNumber: 7);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RegisterPlayerToTeamAsync(players[0], teamBId, tournamentId, jerseyNumber: 8));

        Assert.Contains("two teams in the same tournament", ex.Message);
    }

    /// <summary>
    /// Seeds one tournament, two teams (A and B) and <paramref name="playerCount"/>
    /// players (all created against team A). Returns the tournament id, both
    /// team ids and the player ids.
    /// </summary>
    private static async Task<(Guid TournamentId, Guid TeamAId, Guid TeamBId, List<Guid> Players)> SeedAsync(
        ApplicationDBContext db, int playerCount)
    {
        Guid tournamentId = Guid.NewGuid();

        Team teamA = NewTeam(tournamentId, "Alpha");
        Team teamB = NewTeam(tournamentId, "Bravo");

        Tournament tournament = new()
        {
            Id = tournamentId,
            CreatedBy = "test",
            Name = $"Roster Tournament {tournamentId}",
            Description = "Roster fixture.",
            Slug = $"roster-tournament-{tournamentId}",
            TeamRegistrationDeadline = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            StartDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            Divisions = [],
            Teams = [teamA, teamB],
        };

        db.Tournaments.Add(tournament);

        List<Guid> players = [];
        for (int i = 0; i < playerCount; i++)
        {
            Player player = NewPlayer(teamA);
            db.Players.Add(player);
            players.Add(player.Id);
        }

        await db.SaveChangesAsync();

        return (tournamentId, teamA.Id, teamB.Id, players);
    }

    private static Team NewTeam(Guid tournamentId, string name)
    {
        Guid id = Guid.NewGuid();
        return new Team
        {
            Id = id,
            CreatedBy = "test",
            Name = $"{name} {id}",
            Slug = $"{name.ToLowerInvariant()}-{id}",
            ThreeLetterCode = name[..3].ToUpperInvariant(),
            LogoUrl = "https://example.test/logo.png",
            ShirtColor = "#123456",
            TournamentId = tournamentId,
            Players = [],
        };
    }

    private static Player NewPlayer(Team team)
    {
        Guid id = Guid.NewGuid();
        return new Player
        {
            Id = id,
            CreatedBy = "test",
            FirstName = "Test",
            LastName = $"Player {id}",
            Slug = $"player-{id}",
            DocumentNumber = id.ToString("N")[..10],
            IsSanctioned = false,
            BirthDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SocialSecurity = "OSDE",
            Team = team,
            TeamId = team.Id,
        };
    }
}
