using Application.DTOs.Tournament.Request;

using Domain.Enums;

using System.Net;
using System.Net.Http.Json;

namespace API.Tests;

/// <summary>
/// Proves the authorization gap found during this session's audit is
/// closed: previously, almost no controller declared [Authorize], so any
/// anonymous caller could reach write endpoints (and the admin
/// player-detail endpoint exposing document number/birth date/phone/
/// social security) directly via the API, bypassing the frontend's
/// role-gated UI entirely. These are real HTTP round trips through
/// CustomWebApplicationFactory, not direct service calls, since
/// [Authorize] only takes effect in the MVC pipeline.
/// </summary>
public class AuthorizationGatingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthorizationGatingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPlayerCompleteData_Anonymous_ReturnsUnauthorized()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"api/players/admin/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Guest is not one of the staff roles that may see full player details.
    /// </summary>
    [Fact]
    public async Task GetPlayerCompleteData_WrongRole_ReturnsForbidden()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Guest);

        HttpResponseMessage response = await client.GetAsync($"api/players/admin/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// Expects not-found (missing player), never a permission error —
    /// proves the role check passed and the request reached the handler.
    /// HU-05: staff is now just Owner/Admin, so Admin stands in for the
    /// former manager roles here.
    /// </summary>
    [Fact]
    public async Task GetPlayerCompleteData_StaffRole_IsAuthorized()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.GetAsync($"api/players/admin/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateTournament_Anonymous_ReturnsUnauthorized()
    {
        HttpClient client = _factory.CreateClient();
        CreateTournamentRequest request = BuildTournamentRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("api/tournaments", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateTournament_WrongRole_ReturnsForbidden()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Guest);
        CreateTournamentRequest request = BuildTournamentRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("api/tournaments", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateTournament_OwnerRole_Succeeds()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Owner);
        CreateTournamentRequest request = BuildTournamentRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("api/tournaments", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBlogPost_Anonymous_ReturnsUnauthorized()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.DeleteAsync($"api/blogposts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Public reads must remain anonymous — this is a regression guard, not
    /// a gap: 404 (not 401/403) proves the request reached the handler
    /// without a role check blocking it.
    /// </summary>
    [Fact]
    public async Task GetTournamentById_Anonymous_StillAllowed()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"api/tournaments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static CreateTournamentRequest BuildTournamentRequest()
    {
        DateTime startDate = DateTime.UtcNow.Date.AddDays(30);

        return new CreateTournamentRequest
        {
            Name = $"Tournament-{Guid.NewGuid()}",
            Description = "Authorization gating test tournament",
            StartDate = startDate,
            TeamRegistrationDeadline = startDate.AddDays(-1),
        };
    }
}
