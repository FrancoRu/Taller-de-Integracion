using Application.Interfaces.Services;

using Domain.Entities.Models;

using Infrastructure.Persistance;

using Microsoft.Extensions.DependencyInjection;

using System.Net;
using System.Net.Http.Json;

namespace API.Tests;

/// <summary>
/// Verifies Player's slug support: PlayerService.CreatePlayerAsync generates a
/// unique slug from the player's full name, duplicate names are disambiguated
/// with a numeric suffix, GetPlayerByIdOrSlugAsync resolves a player by either
/// its GUID id or its slug, and GET api/players/{idOrSlug} resolves by either
/// form. PlayerController has no SupabaseHelper dependency, so this can run as a
/// real HTTP round trip through CustomWebApplicationFactory.
/// </summary>
public class PlayerSlugTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PlayerSlugTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreatePlayerAsync_GeneratesSlugFromFullName()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerService playerService = scope.ServiceProvider.GetRequiredService<IPlayerService>();

        (Team team, Guid tournamentId) = await SeedTeamAsync(db);

        Player created = await playerService.CreatePlayerAsync(
            BuildPlayer(team, "Ñandú", "Pérez"), tournamentId);

        Assert.False(string.IsNullOrWhiteSpace(created.Slug));
        Assert.DoesNotContain(' ', created.Slug);
        Assert.Equal(created.Slug, created.Slug.ToLowerInvariant());
    }

    [Fact]
    public async Task CreatePlayerAsync_DuplicateFullName_AppendsSuffixToSlug()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IPlayerService playerService = scope.ServiceProvider.GetRequiredService<IPlayerService>();

        (Team team, Guid tournamentId) = await SeedTeamAsync(db);

        string firstName = "Juan";
        string lastName = "Duplicado" + Guid.NewGuid().ToString("N")[..8];

        Player first = await playerService.CreatePlayerAsync(
            BuildPlayer(team, firstName, lastName), tournamentId);
        Player second = await playerService.CreatePlayerAsync(
            BuildPlayer(team, firstName, lastName), tournamentId);

        Assert.NotEqual(first.Slug, second.Slug);
        Assert.Equal($"{first.Slug}-2", second.Slug);
    }

    [Fact]
    public async Task GetPlayerById_BySlug_Returns200WithMatchingPlayer()
    {
        Guid createdId;
        string createdSlug;

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            IPlayerService playerService = scope.ServiceProvider.GetRequiredService<IPlayerService>();

            (Team team, Guid tournamentId) = await SeedTeamAsync(db);
            Player created = await playerService.CreatePlayerAsync(
                BuildPlayer(team, "Carlos", "Slug" + Guid.NewGuid().ToString("N")[..8]), tournamentId);

            createdId = created.Id;
            createdSlug = created.Slug;
        }

        HttpClient client = _factory.CreateClient();

        HttpResponseMessage byId = await client.GetAsync($"api/players/{createdId}");
        HttpResponseMessage bySlug = await client.GetAsync($"api/players/{createdSlug}");

        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);
        Assert.Equal(HttpStatusCode.OK, bySlug.StatusCode);

        PlayerIdResponse? bySlugBody = await bySlug.Content.ReadFromJsonAsync<PlayerIdResponse>();
        Assert.NotNull(bySlugBody);
        Assert.Equal(createdId, bySlugBody!.Id);
    }

    [Fact]
    public async Task GetPlayerById_UnknownSlug_Returns404()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"api/players/unknown-slug-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record PlayerIdResponse(Guid Id, string Slug);

    private static Player BuildPlayer(Team team, string firstName, string lastName)
    {
        return new Player
        {
            FirstName = firstName,
            LastName = lastName,
            Slug = null!,
            DocumentNumber = Guid.NewGuid().ToString("N")[..10],
            IsSanctioned = false,
            BirthDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SocialSecurity = "OSDE",
            Team = team,
            TeamId = team.Id,
            CreatedBy = "test",
        };
    }

    private static async Task<(Team Team, Guid TournamentId)> SeedTeamAsync(ApplicationDBContext db)
    {
        DateTime startDate = DateTime.UtcNow.Date;
        Guid teamId = Guid.NewGuid();

        Tournament tournament = new()
        {
            Description = "Player slug characterization tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = startDate.AddDays(-1),
            StartDate = startDate,
            MaxTeams = 8,
            MinTeams = 2,
            Divisions = [],
            Teams = [],
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
            Tournament = tournament,
            TournamentId = tournament.Id,
            Players = [],
            CreatedBy = "test",
        };

        db.Tournaments.Add(tournament);
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        return (team, tournament.Id);
    }
}
