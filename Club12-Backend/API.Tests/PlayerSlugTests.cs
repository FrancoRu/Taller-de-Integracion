using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.Extensions.DependencyInjection;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

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

    /// <summary>
    /// GET api/players/admin/{slug} must resolve the player by its exact slug and
    /// return 200 with the AdminPlayerResponse body. Asserting documentNumber is
    /// present proves the request bound to the admin action and was NOT shadowed
    /// by the public {idOrSlug} route (documentNumber is an AdminPlayerResponse-
    /// only field).
    /// </summary>
    [Fact]
    public async Task GetPlayerByIdCompleteData_BySlug_Returns200WithDocumentNumber()
    {
        Guid createdId;
        string createdSlug;

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            IPlayerService playerService = scope.ServiceProvider.GetRequiredService<IPlayerService>();

            (Team team, Guid tournamentId) = await SeedTeamAsync(db);
            Player created = await playerService.CreatePlayerAsync(
                BuildPlayer(team, "Carlos", "AdminSlug" + Guid.NewGuid().ToString("N")[..8]), tournamentId);

            createdId = created.Id;
            createdSlug = created.Slug;
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage bySlug = await client.GetAsync($"api/players/admin/{createdSlug}");

        Assert.Equal(HttpStatusCode.OK, bySlug.StatusCode);

        AdminPlayerBody? body = await bySlug.Content.ReadFromJsonAsync<AdminPlayerBody>();
        Assert.NotNull(body);
        Assert.Equal(createdId, body!.Id);
        Assert.False(string.IsNullOrWhiteSpace(body.DocumentNumber));
    }

    /// <summary>
    /// Regression for matchPage.tsx:513 — the admin route must keep resolving a
    /// player by its GUID id after the parameter is widened from Guid to string.
    /// </summary>
    [Fact]
    public async Task GetPlayerByIdCompleteData_ByGuid_Returns200()
    {
        Guid createdId;

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            IPlayerService playerService = scope.ServiceProvider.GetRequiredService<IPlayerService>();

            (Team team, Guid tournamentId) = await SeedTeamAsync(db);
            Player created = await playerService.CreatePlayerAsync(
                BuildPlayer(team, "Carlos", "AdminGuid" + Guid.NewGuid().ToString("N")[..8]), tournamentId);

            createdId = created.Id;
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage byId = await client.GetAsync($"api/players/admin/{createdId}");

        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);

        AdminPlayerBody? body = await byId.Content.ReadFromJsonAsync<AdminPlayerBody>();
        Assert.NotNull(body);
        Assert.Equal(createdId, body!.Id);
    }

    /// <summary>
    /// An unknown id-or-slug on the admin route must return a 404 whose body is a
    /// ProblemDetails document (application/problem+json), not a routing-level 404
    /// with an empty body.
    /// </summary>
    [Fact]
    public async Task GetPlayerByIdCompleteData_UnknownSlug_Returns404ProblemJson()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);
        string missingSlug = $"no-such-player-{Guid.NewGuid():N}";

        HttpResponseMessage response = await client.GetAsync($"api/players/admin/{missingSlug}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        // The detail naming the looked-up identifier proves the request reached
        // the action's NotFoundProblem path, not a routing-level 404.
        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains(missingSlug, doc.RootElement.GetProperty("detail").GetString());
    }

    /// <summary>
    /// Slug lookup on the admin route is exact/ordinal — a wrong-case slug is not
    /// normalized and must miss, returning a 404 ProblemDetails (the request still
    /// reaches the handler, so the body is problem+json rather than a routing 404).
    /// </summary>
    [Fact]
    public async Task GetPlayerByIdCompleteData_WrongCaseSlug_Returns404()
    {
        string createdSlug;

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            IPlayerService playerService = scope.ServiceProvider.GetRequiredService<IPlayerService>();

            (Team team, Guid tournamentId) = await SeedTeamAsync(db);
            Player created = await playerService.CreatePlayerAsync(
                BuildPlayer(team, "Carlos", "AdminCase" + Guid.NewGuid().ToString("N")[..8]), tournamentId);

            createdSlug = created.Slug;
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);
        string wrongCaseSlug = createdSlug.ToUpperInvariant();

        HttpResponseMessage response = await client.GetAsync($"api/players/admin/{wrongCaseSlug}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains(wrongCaseSlug, doc.RootElement.GetProperty("detail").GetString());
    }

    private sealed record PlayerIdResponse(Guid Id, string Slug);

    private sealed record AdminPlayerBody(Guid Id, string Slug, string DocumentNumber);

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
