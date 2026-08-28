using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.Extensions.DependencyInjection;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Characterization tests for PlayerSanctionService.GetExpiredSanctionsAsync
/// (PlayerSanctionService.cs:33-36), the date-math predicate
/// `IssuedDate.AddDays(Duration) &lt;= cutoffDate` used to determine which
/// sanctions have expired as of a cutoff date. These tests pin the currently-
/// correct behavior of the existing, unmodified service — pure test
/// additions, no production code changes.
///
/// Known test-harness limitation: SQLite (test provider, via
/// CustomWebApplicationFactory) may translate `DateTime.AddDays` comparisons
/// differently than Npgsql (production). All seed dates below use UTC,
/// whole-second `DateTime` values with whole-day `Duration`, so expiry always
/// lands on an exact instant and SQLite's translation is unambiguous; EF Core's
/// SQLite provider successfully translates this predicate (verified by the
/// GREEN runs of every scenario in this file — no runtime translation failure
/// was observed).
///
/// Each test uses a base date far apart from the others (1000+ day offsets
/// from a fixed anchor) so that seeded sanctions never overlap another test's
/// cutoffDate — GetExpiredSanctionsAsync has no tournament/player scoping, and
/// the CustomWebApplicationFactory database is shared across all tests in this
/// class via IClassFixture.
/// </summary>
public class PlayerSanctionServiceTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly DateTime Anchor = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly CustomWebApplicationFactory _factory;

    public PlayerSanctionServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetExpiredSanctionsAsync_BeforeExpiry_IsExcluded()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerSanctionService sanctionService = scope.ServiceProvider.GetRequiredService<IPlayerSanctionService>();

        DateTime issuedDate = Anchor;
        int duration = 10;
        DateTime expiry = issuedDate.AddDays(duration);
        DateTime cutoffDateBeforeExpiry = expiry.AddDays(-1);

        PlayerSanction sanction = await SeedSanctionAsync(db, issuedDate, duration);

        IEnumerable<PlayerSanction> result = await sanctionService.GetExpiredSanctionsAsync(cutoffDateBeforeExpiry);

        Assert.DoesNotContain(result, s => s.Id == sanction.Id);
    }

    [Fact]
    public async Task GetExpiredSanctionsAsync_ExactlyAtBoundary_IsIncluded()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerSanctionService sanctionService = scope.ServiceProvider.GetRequiredService<IPlayerSanctionService>();

        DateTime issuedDate = Anchor.AddDays(1000);
        int duration = 10;
        DateTime expiry = issuedDate.AddDays(duration);
        DateTime cutoffDateAtExpiryBoundary = expiry;

        PlayerSanction sanction = await SeedSanctionAsync(db, issuedDate, duration);

        IEnumerable<PlayerSanction> result = await sanctionService.GetExpiredSanctionsAsync(cutoffDateAtExpiryBoundary);

        Assert.Contains(result, s => s.Id == sanction.Id);
    }

    [Fact]
    public async Task GetExpiredSanctionsAsync_WellBeforeCutoff_IsIncluded()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerSanctionService sanctionService = scope.ServiceProvider.GetRequiredService<IPlayerSanctionService>();

        DateTime issuedDate = Anchor.AddDays(2000);
        int duration = 5;
        DateTime expiry = issuedDate.AddDays(duration);
        DateTime cutoffDateWellAfterExpiry = expiry.AddDays(20);

        PlayerSanction sanction = await SeedSanctionAsync(db, issuedDate, duration);

        IEnumerable<PlayerSanction> result = await sanctionService.GetExpiredSanctionsAsync(cutoffDateWellAfterExpiry);

        Assert.Contains(result, s => s.Id == sanction.Id);
    }

    /// <summary>
    /// cutoffDate precedes Anchor, the earliest issuedDate used by any test in
    /// this class (all others use Anchor plus a non-negative offset), so it
    /// excludes both this test's own not-yet-expired sanction and every other
    /// test's already-expired sanction. GetExpiredSanctionsAsync has no
    /// tournament/player scoping, and the CustomWebApplicationFactory database
    /// is shared across all tests in this class via IClassFixture.
    /// </summary>
    [Fact]
    public async Task GetExpiredSanctionsAsync_NoMatches_ReturnsEmpty()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerSanctionService sanctionService = scope.ServiceProvider.GetRequiredService<IPlayerSanctionService>();

        DateTime issuedDate = Anchor.AddDays(3000);
        int duration = 10;
        DateTime cutoffDate = Anchor.AddDays(-1);

        await SeedSanctionAsync(db, issuedDate, duration);

        IEnumerable<PlayerSanction> result = await sanctionService.GetExpiredSanctionsAsync(cutoffDate);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetExpiredSanctionsAsync_IncludesPlayerNavigation()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerSanctionService sanctionService = scope.ServiceProvider.GetRequiredService<IPlayerSanctionService>();

        DateTime issuedDate = Anchor.AddDays(4000);
        int duration = 10;
        DateTime expiry = issuedDate.AddDays(duration);
        DateTime cutoffDate = expiry;

        PlayerSanction sanction = await SeedSanctionAsync(db, issuedDate, duration);

        IEnumerable<PlayerSanction> result = await sanctionService.GetExpiredSanctionsAsync(cutoffDate);

        PlayerSanction returned = Assert.Single(result, s => s.Id == sanction.Id);
        Assert.NotNull(returned.Player);
        Assert.Equal(sanction.Player.Id, returned.Player.Id);
        Assert.Equal(sanction.Player.DocumentNumber, returned.Player.DocumentNumber);
    }

    /// <summary>
    /// Seeds the full object graph a PlayerSanction requires under SQLite's
    /// enforced FKs: Tournament→Division→Stage→Match and Team→Player, mirroring
    /// PlayerSanctionAppealTests.SeedSanctionAsync.
    /// </summary>
    private static async Task<PlayerSanction> SeedSanctionAsync(
        ApplicationDBContext db, DateTime issuedDate, int duration)
    {
        Guid divisionId = Guid.NewGuid();
        Guid stageId = Guid.NewGuid();
        Guid teamId = Guid.NewGuid();

        Tournament tournament = new()
        {
            Description = "Sanction expiry characterization tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = issuedDate.AddDays(-1),
            StartDate = issuedDate,
            MaxTeams = 8,
            MinTeams = 2,
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };

        Division division = new()
        {
            Slug = $"division-{Guid.NewGuid()}",
            Id = divisionId,
            Name = $"Division-{Guid.NewGuid()}",
            Tournament = tournament,
            Stages = [],
            CreatedBy = "test",
        };

        Stage stage = new()
        {
            Slug = $"stage-{Guid.NewGuid()}",
            Id = stageId,
            Name = $"Stage-{Guid.NewGuid()}",
            StageType = StageType.Group,
            IsActive = true,
            StartDate = issuedDate,
            EndDate = issuedDate.AddDays(14),
            DivisionId = divisionId,
            Division = division,
            Matches = [],
            CreatedBy = "test",
        };

        Team team = new()
        {
            Id = teamId,
            Name = $"Team-{Guid.NewGuid()}",
            Slug = $"team-{Guid.NewGuid()}",
            ThreeLetterCode = "SEX",
            LogoUrl = "https://example.test/logo.png",
            ShirtColor = "Blue",
            Players = [],
            CreatedBy = "test",
        };

        Player player = new()
        {
            Slug = $"player-{Guid.NewGuid()}",
            FirstName = "Expiry",
            LastName = "Tester",
            DocumentNumber = Guid.NewGuid().ToString("N")[..10],
            IsSanctioned = true,
            BirthDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SocialSecurity = "OSDE",
            Team = team,
            TeamId = teamId,
            CreatedBy = "test",
        };

        Match match = new()
        {
            MatchDate = issuedDate,
            Type = MatchType.Regular,
            Slug = $"match-{Guid.NewGuid()}",
            IsFinished = true,
            Stage = stage,
            StageId = stageId,
            CreatedBy = "test",
        };

        PlayerSanction sanction = new()
        {
            Duration = duration,
            IssuedDate = issuedDate,
            Description = "Sanction expiry characterization test",
            Slug = $"sanction-{Guid.NewGuid()}",
            Player = player,
            Match = match,
            CreatedBy = "test",
        };

        db.Tournaments.Add(tournament);
        db.Divisions.Add(division);
        db.Stages.Add(stage);
        db.Teams.Add(team);
        db.Players.Add(player);
        db.Matches.Add(match);
        db.PlayerSanctions.Add(sanction);
        await db.SaveChangesAsync();

        return sanction;
    }
}
