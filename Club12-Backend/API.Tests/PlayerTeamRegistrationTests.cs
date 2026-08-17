using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Verifies season-scoped roster membership: PlayerTeamRegistration is the
/// source of truth for "was this player on this team during this season",
/// decoupled from Player.TeamId (a denormalized "current team" pointer) the
/// same way StageTeamMatch decouples "which teams are in which stage" from
/// any fixed property on Team. Exercises the real IPlayerService/ITeamService
/// capabilities through a DI scope against the CustomWebApplicationFactory's
/// SQLite-backed ApplicationDBContext, matching this project's established
/// integration-test style (see DivisionTournamentReassignmentTests,
/// ScorerRepositoryTests).
/// </summary>
public class PlayerTeamRegistrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PlayerTeamRegistrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetTeamByIdAsync_TeamMovedToNewTournament_OldSeasonRosterNoLongerShowsForCurrentTournament()
    {
        using IServiceScope seedScope = _factory.Services.CreateScope();
        ApplicationDBContext seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Tournament apertura = await SeedTournamentAsync(seedDb, "Apertura");
        Tournament clausura = await SeedTournamentAsync(seedDb, "Clausura");
        Team team = await SeedTeamAsync(seedDb, apertura.Id);
        Player player = await SeedPlayerAsync(seedDb, team);
        await SeedRegistrationAsync(seedDb, player, team, apertura);

        using (IServiceScope readScope = _factory.Services.CreateScope())
        {
            ITeamService teamService = readScope.ServiceProvider.GetRequiredService<ITeamService>();

            Team? currentSeasonTeam = await teamService.GetTeamByIdAsync(team.Id);
            Assert.NotNull(currentSeasonTeam);
            Assert.Contains(currentSeasonTeam!.Players, p => p.Id == player.Id);
        }

        // Reassign the team to a new season, reusing the same Team row —
        // mirrors TeamService.RegisterTeamsToTournamentAsync, the mechanism
        // that actually split the real "Apertura"/"Femenino" tournaments.
        using (IServiceScope moveScope = _factory.Services.CreateScope())
        {
            ApplicationDBContext moveDb = moveScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            Team trackedTeam = await moveDb.Teams.SingleAsync(t => t.Id == team.Id);
            trackedTeam.TournamentId = clausura.Id;
            await moveDb.SaveChangesAsync();
        }

        using (IServiceScope verifyScope = _factory.Services.CreateScope())
        {
            ITeamService teamService = verifyScope.ServiceProvider.GetRequiredService<ITeamService>();

            Team? nowOnClausura = await teamService.GetTeamByIdAsync(team.Id);
            Assert.NotNull(nowOnClausura);
            Assert.DoesNotContain(nowOnClausura!.Players, p => p.Id == player.Id);

            Team? pastApertura = await teamService.GetTeamByIdAsync(team.Id, apertura.Id);
            Assert.NotNull(pastApertura);
            Assert.Contains(pastApertura!.Players, p => p.Id == player.Id);
        }
    }

    [Fact]
    public async Task CreatePlayerAsync_RegistersPlayerToTeamsCurrentTournament()
    {
        using IServiceScope seedScope = _factory.Services.CreateScope();
        ApplicationDBContext seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Tournament apertura = await SeedTournamentAsync(seedDb, "Apertura");
        Team team = await SeedTeamAsync(seedDb, apertura.Id);

        // Team is intentionally left unset (null!) rather than pointing at
        // the detached `team` instance from the seed scope's own
        // DbContext: that's exactly what AutoMapper produces in production
        // (CreatePlayerRequest has no Team property, only TeamId, so
        // CreateMap<CreatePlayerRequest, Player> never touches the Team
        // nav) and avoids EF treating an unrelated detached object graph
        // (Team -> Tournament) as new rows to insert.
        Player newPlayer = new()
        {
            FirstName = "Nuevo",
            LastName = "Jugador",
            DocumentNumber = Guid.NewGuid().ToString("N")[..10],
            IsSanctioned = false,
            BirthDate = new DateTime(2001, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            SocialSecurity = "OSDE",
            Team = null!,
            TeamId = team.Id,
            CreatedBy = "test",
        };

        using (IServiceScope actScope = _factory.Services.CreateScope())
        {
            IPlayerService playerService = actScope.ServiceProvider.GetRequiredService<IPlayerService>();
            await playerService.CreatePlayerAsync(newPlayer, apertura.Id);
        }

        using IServiceScope verifyScope = _factory.Services.CreateScope();
        ApplicationDBContext verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        PlayerTeamRegistration? registration = await verifyDb.PlayerTeamRegistrations
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.PlayerId == newPlayer.Id);

        Assert.NotNull(registration);
        Assert.Equal(team.Id, registration!.TeamId);
        Assert.Equal(apertura.Id, registration.TournamentId);

        ITeamService teamService = verifyScope.ServiceProvider.GetRequiredService<ITeamService>();
        Team? teamWithRoster = await teamService.GetTeamByIdAsync(team.Id);
        Assert.Contains(teamWithRoster!.Players, p => p.Id == newPlayer.Id);
    }

    [Fact]
    public async Task UpdatePlayerAsync_TeamChangedWithinSameSeason_MovesRegistrationInstead_OfDuplicating()
    {
        using IServiceScope seedScope = _factory.Services.CreateScope();
        ApplicationDBContext seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Tournament apertura = await SeedTournamentAsync(seedDb, "Apertura");
        Team originalTeam = await SeedTeamAsync(seedDb, apertura.Id);
        Team newTeam = await SeedTeamAsync(seedDb, apertura.Id);
        Player player = await SeedPlayerAsync(seedDb, originalTeam);
        await SeedRegistrationAsync(seedDb, player, originalTeam, apertura);

        using (IServiceScope actScope = _factory.Services.CreateScope())
        {
            IPlayerService playerService = actScope.ServiceProvider.GetRequiredService<IPlayerService>();
            ApplicationDBContext actDb = actScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

            Player tracked = await actDb.Players.SingleAsync(p => p.Id == player.Id);
            tracked.TeamId = newTeam.Id;

            await playerService.UpdatePlayerAsync(tracked, apertura.Id);
        }

        using IServiceScope verifyScope = _factory.Services.CreateScope();
        ApplicationDBContext verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        List<PlayerTeamRegistration> registrations = await verifyDb.PlayerTeamRegistrations
            .AsNoTracking()
            .Where(r => r.PlayerId == player.Id)
            .ToListAsync();

        PlayerTeamRegistration onlyRegistration = Assert.Single(registrations);
        Assert.Equal(newTeam.Id, onlyRegistration.TeamId);
        Assert.Equal(apertura.Id, onlyRegistration.TournamentId);
    }

    /// <summary>
    /// Replicates, as an EF/LINQ query, the exact backfill join performed by
    /// the 20260817082125_AddPlayerTeamRegistrationTable migration's raw SQL
    /// (Players JOIN Teams ON Players.TeamId, attributing each player to
    /// their team's CURRENT TournamentId, skipping teams with none). The raw
    /// SQL itself was verified end-to-end against a disposable local
    /// Postgres container seeded with data shaped exactly like the real
    /// split (one team on an "Apertura"-named tournament, one on a
    /// "Femenino"-named tournament) — this test pins the same logic inside
    /// the automated suite using the SQLite-backed harness.
    /// </summary>
    [Fact]
    public async Task Backfill_JoinLogic_AttributesEachPlayerToTheirTeamsCurrentTournament()
    {
        using IServiceScope seedScope = _factory.Services.CreateScope();
        ApplicationDBContext seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Tournament apertura = await SeedTournamentAsync(seedDb, "Apertura Club 12 2026");
        Tournament femenino = await SeedTournamentAsync(seedDb, "Femenino Club12 La Vuelta");
        Team aperturaTeam = await SeedTeamAsync(seedDb, apertura.Id);
        Team femeninoTeam = await SeedTeamAsync(seedDb, femenino.Id);
        Player aperturaPlayer = await SeedPlayerAsync(seedDb, aperturaTeam);
        Player femeninoPlayer = await SeedPlayerAsync(seedDb, femeninoTeam);

        // The exact join the migration's Up() runs in raw SQL, expressed in LINQ:
        List<(Guid PlayerId, Guid TeamId, Guid TournamentId)> backfillRows = await seedDb.Players
            .Join(seedDb.Teams, p => p.TeamId, t => t.Id, (p, t) => new { Player = p, Team = t })
            .Where(pt => pt.Team.TournamentId != null)
            .Select(pt => new ValueTuple<Guid, Guid, Guid>(pt.Player.Id, pt.Team.Id, pt.Team.TournamentId!.Value))
            .ToListAsync();

        Assert.Contains(backfillRows, r => r.PlayerId == aperturaPlayer.Id && r.TournamentId == apertura.Id);
        Assert.Contains(backfillRows, r => r.PlayerId == femeninoPlayer.Id && r.TournamentId == femenino.Id);
        Assert.DoesNotContain(backfillRows, r => r.PlayerId == aperturaPlayer.Id && r.TournamentId == femenino.Id);
        Assert.DoesNotContain(backfillRows, r => r.PlayerId == femeninoPlayer.Id && r.TournamentId == apertura.Id);
    }

    private static async Task<Tournament> SeedTournamentAsync(ApplicationDBContext db, string name)
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        Tournament tournament = new()
        {
            Description = $"{name} description",
            Name = $"{name}-{Guid.NewGuid()}",
            Slug = $"{name.ToLowerInvariant().Replace(' ', '-')}-{Guid.NewGuid()}",
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

    private static async Task<Team> SeedTeamAsync(ApplicationDBContext db, Guid? tournamentId)
    {
        Team team = new()
        {
            Name = $"Team-{Guid.NewGuid()}",
            Slug = $"team-{Guid.NewGuid()}",
            ThreeLetterCode = Guid.NewGuid().ToString("N")[..3].ToUpperInvariant(),
            LogoUrl = "https://example.test/logo.png",
            ShirtColor = "Green",
            TournamentId = tournamentId,
            Players = [],
            CreatedBy = "test",
        };

        db.Teams.Add(team);
        await db.SaveChangesAsync();

        return team;
    }

    private static async Task<Player> SeedPlayerAsync(ApplicationDBContext db, Team team)
    {
        Player player = new()
        {
            FirstName = "Test",
            LastName = "Player",
            DocumentNumber = Guid.NewGuid().ToString("N")[..10],
            IsSanctioned = false,
            BirthDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SocialSecurity = "OSDE",
            Team = team,
            TeamId = team.Id,
            CreatedBy = "test",
        };

        db.Players.Add(player);
        await db.SaveChangesAsync();

        return player;
    }

    private static async Task<PlayerTeamRegistration> SeedRegistrationAsync(
        ApplicationDBContext db, Player player, Team team, Tournament tournament)
    {
        PlayerTeamRegistration registration = new()
        {
            PlayerId = player.Id,
            TeamId = team.Id,
            TournamentId = tournament.Id,
            CreatedBy = "test",
        };

        db.PlayerTeamRegistrations.Add(registration);
        await db.SaveChangesAsync();

        return registration;
    }
}
