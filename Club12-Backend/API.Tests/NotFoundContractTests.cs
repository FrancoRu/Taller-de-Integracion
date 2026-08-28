using Application.DTOs.Divisions.Request;
using Application.DTOs.Player.Request;
using Application.DTOs.Tournament.Request;

using Domain.Enums;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace API.Tests;

/// <summary>
/// Characterization/contract test proving every "resource not found" lookup across the 9
/// converted controllers returns 404 with a ProblemDetails-consistent body (matching the
/// shape GlobalExceptionHandler already emits for unhandled
/// exceptions), instead of the legacy bare-string 400. Also proves the create-time FK
/// validation (Player POST referencing a missing TeamId) deliberately stays 400 — it is
/// invalid input, not a not-found lookup.
/// </summary>
public class NotFoundContractTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public NotFoundContractTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// One GET-by-id route per controller, for the controllers whose full dependency graph
    /// can boot through CustomWebApplicationFactory anonymously (6 of the 9 —
    /// see SupabaseDependentControllerNotFoundTests for Team/Venue/BlogPost, which are
    /// covered via direct-controller unit tests instead; see that class' doc comment for why).
    /// </summary>
    public static readonly TheoryData<string> NotFoundGetByIdRoutes =
    [
        "api/matches/{0}",
        "api/divisions/{0}/detail",
        "api/players/{0}",
        "api/player-sanctions/{0}",
        "api/player-statistics/{0}",
        "api/tournaments/{0}",
    ];

    [Theory]
    [MemberData(nameof(NotFoundGetByIdRoutes))]
    public async Task GetById_MissingEntity_Returns404ProblemDetails(string routeTemplate)
    {
        HttpClient client = _factory.CreateClient();
        Guid missingId = Guid.NewGuid();
        string url = string.Format(routeTemplate, missingId);

        HttpResponseMessage response = await client.GetAsync(url);
        string debugBody = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.NotFound, $"url={url} status={response.StatusCode} body={debugBody}");
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using JsonDocument doc = JsonDocument.Parse(debugBody);
        JsonElement root = doc.RootElement;

        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("detail").GetString()));
        Assert.Equal(404, root.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
    }

    /// <summary>
    /// Covers the spec's second scenario for this requirement ("PUT/DELETE or nested action
    /// against nonexistent parent returns 404") for the PUT-by-id case: a full HTTP-round-trip
    /// PUT against a nonexistent division id must return 404 + ProblemDetails, not 400. Prior
    /// to this test, every not-found case in this suite was GET-shaped only, even though the
    /// PUT/DELETE/nested-action sites were converted alongside the GET sites in Phase 2.
    /// </summary>
    [Fact]
    public async Task UpdateDivisionById_MissingEntity_Returns404ProblemDetails()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Owner);
        Guid missingId = Guid.NewGuid();
        UpdateDivisionRequest request = new() { Name = "Nonexistent Division", IsFinished = false };

        HttpResponseMessage response = await client.PutAsJsonAsync($"api/divisions/{missingId}", request);

        await AssertNotFoundProblemDetailsAsync(response);
    }

    /// <summary>
    /// Covers the spec's "nested action referencing a nonexistent parent id" case (the spec's
    /// own example is "adding a sanction to a nonexistent player"): registering teams to a
    /// nonexistent tournament must return 404 + ProblemDetails via a real HTTP round trip.
    /// </summary>
    [Fact]
    public async Task RegisterTeam_MissingTournament_Returns404ProblemDetails()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Owner);
        Guid missingTournamentId = Guid.NewGuid();
        RegisterTeamsInTournamentRequest request = new() { TeamIds = [Guid.NewGuid()] };

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/tournaments/register-teams/{missingTournamentId}", request);

        await AssertNotFoundProblemDetailsAsync(response);
    }

    private static async Task AssertNotFoundProblemDetailsAsync(HttpResponseMessage response)
    {
        string debugBody = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.NotFound, $"status={response.StatusCode} body={debugBody}");
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using JsonDocument doc = JsonDocument.Parse(debugBody);
        JsonElement root = doc.RootElement;

        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("detail").GetString()));
        Assert.Equal(404, root.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
    }

    /// <summary>
    /// Regression guard: the Player POST create-time FK check (missing TeamId) is
    /// invalid input, not a "resource not found" lookup, and must stay 400 — this change
    /// must NOT touch it.
    /// </summary>
    [Fact]
    public async Task CreatePlayer_MissingTeamId_StaysBadRequest()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Owner);
        CreatePlayerRequest request = new()
        {
            FirstName = "Test",
            LastName = "Player",
            DocumentNumber = Guid.NewGuid().ToString("N")[..10],
            BirthDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            PhoneNumber = "1234567890",
            SocialSecurity = "OSDE",
            TeamId = Guid.NewGuid(),
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("api/players/", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
