using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using System.Net;
using System.Net.Http.Json;

using MatchType = Domain.Enums.MatchType;

namespace API.Tests;

/// <summary>
/// Verifies PlayerSanction's slug support: PlayerSanctionService.CreatePlayerSanctionAsync
/// generates a unique slug from the sanctioned player's name and the sanction's issued date
/// ("{playerFullName} {issuedDate:yyyy-MM-dd}"), and GET api/player-sanctions/{idOrSlug}
/// resolves a sanction by either its GUID id or its slug. Unlike BlogPost/Team/Venue,
/// PlayerSanctionController has no SupabaseHelper dependency, so this can run as a real
/// HTTP round trip through CustomWebApplicationFactory.
/// </summary>
public class PlayerSanctionSlugTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PlayerSanctionSlugTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreatePlayerSanctionAsync_GeneratesSlugFromPlayerNameAndIssuedDate()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerSanctionService sanctionService = scope.ServiceProvider.GetRequiredService<IPlayerSanctionService>();

        (Player player, Match match) = await SeedPlayerAndMatchAsync(db);
        DateTime issuedDate = new(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);

        PlayerSanction created = await sanctionService.CreatePlayerSanctionAsync(new PlayerSanction
        {
            Duration = 1,
            IssuedDate = issuedDate,
            Description = "Slug generation test",
            Slug = null!,
            Player = player,
            PlayerId = player.Id,
            Match = match,
            MatchId = match.Id,
            CreatedBy = "test",
        });

        Assert.False(string.IsNullOrWhiteSpace(created.Slug));
        Assert.Equal(created.Slug, created.Slug.ToLowerInvariant());
        Assert.EndsWith("2026-03-15", created.Slug);
    }

    [Fact]
    public async Task CreatePlayerSanctionAsync_SamePlayerSameDay_AppendsSuffixToSlug()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerSanctionService sanctionService = scope.ServiceProvider.GetRequiredService<IPlayerSanctionService>();

        (Player player, Match match) = await SeedPlayerAndMatchAsync(db);
        DateTime issuedDate = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        PlayerSanction first = await sanctionService.CreatePlayerSanctionAsync(new PlayerSanction
        {
            Duration = 1,
            IssuedDate = issuedDate,
            Description = "First sanction same day",
            Slug = null!,
            Player = player,
            PlayerId = player.Id,
            Match = match,
            MatchId = match.Id,
            CreatedBy = "test",
        });

        PlayerSanction second = await sanctionService.CreatePlayerSanctionAsync(new PlayerSanction
        {
            Duration = 1,
            IssuedDate = issuedDate,
            Description = "Second sanction same day",
            Slug = null!,
            Player = player,
            PlayerId = player.Id,
            Match = match,
            MatchId = match.Id,
            CreatedBy = "test",
        });

        Assert.NotEqual(first.Slug, second.Slug);
        Assert.Equal($"{first.Slug}-2", second.Slug);
    }

    [Fact]
    public async Task GetPlayerSanctionById_BySlug_Returns200WithMatchingSanction()
    {
        using IServiceScope seedScope = _factory.Services.CreateScope();
        ApplicationDBContext db = seedScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerSanctionService sanctionService = seedScope.ServiceProvider.GetRequiredService<IPlayerSanctionService>();

        (Player player, Match match) = await SeedPlayerAndMatchAsync(db);
        PlayerSanction created = await sanctionService.CreatePlayerSanctionAsync(new PlayerSanction
        {
            Duration = 2,
            IssuedDate = DateTime.UtcNow.Date,
            Description = "Retrieved by slug",
            Slug = null!,
            Player = player,
            PlayerId = player.Id,
            Match = match,
            MatchId = match.Id,
            CreatedBy = "test",
        });

        HttpClient client = _factory.CreateClient();

        HttpResponseMessage byId = await client.GetAsync($"api/player-sanctions/{created.Id}");
        HttpResponseMessage bySlug = await client.GetAsync($"api/player-sanctions/{created.Slug}");

        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);
        Assert.Equal(HttpStatusCode.OK, bySlug.StatusCode);

        SanctionIdResponse? bySlugBody = await bySlug.Content.ReadFromJsonAsync<SanctionIdResponse>();
        Assert.NotNull(bySlugBody);
        Assert.Equal(created.Id, bySlugBody!.Id);
    }

    [Fact]
    public async Task GetPlayerSanctionById_UnknownSlug_Returns404()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"api/player-sanctions/unknown-slug-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record SanctionIdResponse(Guid Id);

    /// <summary>
    /// Seeds the minimal object graph a PlayerSanction requires under SQLite's enforced FKs
    /// (Team→Player and Tournament→Division→Stage→Match), mirroring
    /// PlayerSanctionAppealTests.SeedSanctionAsync but stopping short of creating the
    /// PlayerSanction itself, since these tests exercise PlayerSanctionService.CreatePlayerSanctionAsync
    /// directly.
    /// </summary>
    private static async Task<(Player Player, Match Match)> SeedPlayerAndMatchAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date;
        DateTime endDate = startDate.AddDays(14);

        Guid divisionId = Guid.NewGuid();
        Guid stageId = Guid.NewGuid();
        Guid teamId = Guid.NewGuid();

        Tournament tournament = new()
        {
            Description = "Sanction slug characterization tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = startDate.AddDays(-1),
            StartDate = startDate,
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
            StartDate = startDate,
            EndDate = endDate,
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
            ThreeLetterCode = "SLG",
            LogoUrl = "https://example.test/logo.png",
            ShirtColor = "Green",
            Players = [],
            CreatedBy = "test",
        };

        Player player = new()
        {
            Slug = $"player-{Guid.NewGuid()}",
            FirstName = "Perez",
            LastName = "Juan",
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
            MatchDate = startDate,
            Type = MatchType.Regular,
            Slug = $"match-{Guid.NewGuid()}",
            IsFinished = true,
            Stage = stage,
            StageId = stageId,
            CreatedBy = "test",
        };

        db.Tournaments.Add(tournament);
        db.Divisions.Add(division);
        db.Stages.Add(stage);
        db.Teams.Add(team);
        db.Players.Add(player);
        db.Matches.Add(match);
        await db.SaveChangesAsync();

        return (player, match);
    }
}
