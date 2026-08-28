using Application.DTOs.Roster.Response;
using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Verifies copying a roster from a previous season into a new season (HU-53):
/// every source player gets a fresh season registration on the target team
/// (reusing the same Player rows), medical records are NOT inherited — they
/// start Pending (HU-59) — sanctions are never carried over, and re-running the
/// copy is idempotent. Exercises the real <see cref="IRosterCopyService"/>
/// through DI scopes against the CustomWebApplicationFactory's SQLite-backed
/// ApplicationDBContext.
/// </summary>
public class RosterCopyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RosterCopyTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Each player registered to the source team/season gets a new registration
    /// on the target team for the target season, reusing the same Player row.
    /// </summary>
    [Fact]
    public async Task CopyRoster_CreatesTargetRegistrationForEachSourcePlayer()
    {
        using IServiceScope seedScope = _factory.Services.CreateScope();
        ApplicationDBContext seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Tournament sourceSeason = await SeedTournamentAsync(seedDb, "Prev season");
        Tournament targetSeason = await SeedTournamentAsync(seedDb, "New season");
        Team sourceTeam = await SeedTeamAsync(seedDb, sourceSeason.Id);
        Team targetTeam = await SeedTeamAsync(seedDb, targetSeason.Id);

        Player playerA = await SeedPlayerAsync(seedDb, sourceTeam);
        Player playerB = await SeedPlayerAsync(seedDb, sourceTeam);
        await SeedRegistrationAsync(seedDb, playerA, sourceTeam, sourceSeason);
        await SeedRegistrationAsync(seedDb, playerB, sourceTeam, sourceSeason);

        int playersBefore = await CountPlayersAsync();

        RosterCopyResult result = await CopyAsync(sourceTeam.Id, sourceSeason.Id, targetTeam.Id, targetSeason.Id);

        Assert.Equal(2, result.CopiedCount);
        Assert.Equal(0, result.SkippedCount);

        List<PlayerTeamRegistration> targetRegistrations = await ReadRegistrationsAsync(targetTeam.Id, targetSeason.Id);
        Assert.Equal(2, targetRegistrations.Count);
        Assert.Contains(targetRegistrations, r => r.PlayerId == playerA.Id);
        Assert.Contains(targetRegistrations, r => r.PlayerId == playerB.Id);

        // Same person reused — the copy creates no new Player rows.
        Assert.Equal(playersBefore, await CountPlayersAsync());
    }

    /// <summary>
    /// The medical record (ficha) is not inherited: even when the source
    /// registration was Approved with an uploaded file and a dorsal, the target
    /// registration starts Pending, with no file and no dorsal (HU-59). And the
    /// copy creates no sanctions.
    /// </summary>
    [Fact]
    public async Task CopyRoster_DoesNotCopyMedicalRecordOrSanctions()
    {
        using IServiceScope seedScope = _factory.Services.CreateScope();
        ApplicationDBContext seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Tournament sourceSeason = await SeedTournamentAsync(seedDb, "Prev season");
        Tournament targetSeason = await SeedTournamentAsync(seedDb, "New season");
        Team sourceTeam = await SeedTeamAsync(seedDb, sourceSeason.Id);
        Team targetTeam = await SeedTeamAsync(seedDb, targetSeason.Id);

        Player player = await SeedPlayerAsync(seedDb, sourceTeam);
        PlayerTeamRegistration sourceRegistration = await SeedRegistrationAsync(seedDb, player, sourceTeam, sourceSeason);

        // Source has a fully-approved ficha with a file and a dorsal.
        sourceRegistration.MedicalRecordStatus = MedicalRecordStatus.Approved;
        sourceRegistration.MedicalRecordFileUrl = "medical-records/prev.pdf";
        sourceRegistration.MedicalRecordFileName = "prev.pdf";
        sourceRegistration.JerseyNumber = 10;
        await seedDb.SaveChangesAsync();

        int sanctionsBefore = await CountSanctionsAsync();

        await CopyAsync(sourceTeam.Id, sourceSeason.Id, targetTeam.Id, targetSeason.Id);

        PlayerTeamRegistration target = Assert.Single(await ReadRegistrationsAsync(targetTeam.Id, targetSeason.Id));
        Assert.Equal(MedicalRecordStatus.Pending, target.MedicalRecordStatus);
        Assert.Null(target.MedicalRecordFileUrl);
        Assert.Null(target.MedicalRecordFileName);
        Assert.Null(target.MedicalRecordReviewedAt);
        Assert.Null(target.JerseyNumber);

        // The copy created no sanctions.
        Assert.Equal(sanctionsBefore, await CountSanctionsAsync());
    }

    /// <summary>
    /// Re-running the copy does not duplicate registrations: a source player
    /// already registered to the target season is skipped.
    /// </summary>
    [Fact]
    public async Task CopyRoster_IsIdempotent_DoesNotDuplicateExistingRegistrations()
    {
        using IServiceScope seedScope = _factory.Services.CreateScope();
        ApplicationDBContext seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Tournament sourceSeason = await SeedTournamentAsync(seedDb, "Prev season");
        Tournament targetSeason = await SeedTournamentAsync(seedDb, "New season");
        Team sourceTeam = await SeedTeamAsync(seedDb, sourceSeason.Id);
        Team targetTeam = await SeedTeamAsync(seedDb, targetSeason.Id);

        Player player = await SeedPlayerAsync(seedDb, sourceTeam);
        await SeedRegistrationAsync(seedDb, player, sourceTeam, sourceSeason);

        RosterCopyResult first = await CopyAsync(sourceTeam.Id, sourceSeason.Id, targetTeam.Id, targetSeason.Id);
        Assert.Equal(1, first.CopiedCount);

        RosterCopyResult second = await CopyAsync(sourceTeam.Id, sourceSeason.Id, targetTeam.Id, targetSeason.Id);
        Assert.Equal(0, second.CopiedCount);
        Assert.Equal(1, second.SkippedCount);

        // Exactly one registration for the player in the target season.
        Assert.Single(await ReadRegistrationsAsync(targetTeam.Id, targetSeason.Id));
    }

    private async Task<RosterCopyResult> CopyAsync(
        Guid sourceTeamId, Guid sourceTournamentId, Guid targetTeamId, Guid targetTournamentId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IRosterCopyService rosterCopyService = scope.ServiceProvider.GetRequiredService<IRosterCopyService>();
        return await rosterCopyService.CopyRosterAsync(sourceTeamId, sourceTournamentId, targetTeamId, targetTournamentId);
    }

    private async Task<List<PlayerTeamRegistration>> ReadRegistrationsAsync(Guid teamId, Guid tournamentId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        return await db.PlayerTeamRegistrations
            .AsNoTracking()
            .Where(r => r.TeamId == teamId && r.TournamentId == tournamentId)
            .ToListAsync();
    }

    private async Task<int> CountPlayersAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        return await db.Players.AsNoTracking().CountAsync();
    }

    private async Task<int> CountSanctionsAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        return await db.PlayerSanctions.AsNoTracking().CountAsync();
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
            Status = TournamentStatus.OpenForRegistration,
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
            FirstName = "First",
            LastName = $"Last-{Guid.NewGuid()}",
            Slug = $"player-{Guid.NewGuid()}",
            DocumentNumber = Guid.NewGuid().ToString("N")[..8],
            IsSanctioned = false,
            BirthDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SocialSecurity = "OSDE",
            Team = team,
            TeamId = team.Id,
            PlayerTeamRegistrations = [],
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
