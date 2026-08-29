using Application.DTOs.Season.Request;

using Domain.Enums;

using System.Net;
using System.Net.Http.Json;

namespace API.Tests;

/// <summary>
/// Proves SeasonController's authorization contract: reads are public
/// (AllowAnonymous) while writes require a staff role (Owner or Admin). These
/// are real HTTP round trips through CustomWebApplicationFactory, since
/// [Authorize] only takes effect in the MVC pipeline — mirroring
/// AuthorizationGatingTests.
/// </summary>
public class SeasonControllerAuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SeasonControllerAuthorizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static CreateSeasonRequest BuildSeasonRequest() => new()
    {
        Name = $"Temporada {Guid.NewGuid():N}",
        Year = 2026,
    };

    [Fact]
    public async Task GetAllSeasons_Anonymous_ReturnsOk()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("api/seasons");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateSeason_Anonymous_ReturnsUnauthorized()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("api/seasons", BuildSeasonRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateSeason_GuestRole_ReturnsForbidden()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Guest);

        HttpResponseMessage response = await client.PostAsJsonAsync("api/seasons", BuildSeasonRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.Owner)]
    public async Task CreateSeason_StaffRole_Succeeds(string role)
    {
        HttpClient client = _factory.CreateAuthenticatedClient(role);

        HttpResponseMessage response = await client.PostAsJsonAsync("api/seasons", BuildSeasonRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateThenGetSeasonBySlug_IsPublicAndRoundTrips()
    {
        HttpClient staffClient = _factory.CreateAuthenticatedClient(Roles.Admin);
        CreateSeasonRequest request = BuildSeasonRequest();

        HttpResponseMessage created = await staffClient.PostAsJsonAsync("api/seasons", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        SeasonBody? body = await created.Content.ReadFromJsonAsync<SeasonBody>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Slug));

        // Public read by slug works without any authentication.
        HttpClient anonymous = _factory.CreateClient();
        HttpResponseMessage bySlug = await anonymous.GetAsync($"api/seasons/{body.Slug}");

        Assert.Equal(HttpStatusCode.OK, bySlug.StatusCode);
        SeasonBody? bySlugBody = await bySlug.Content.ReadFromJsonAsync<SeasonBody>();
        Assert.NotNull(bySlugBody);
        Assert.Equal(body.Id, bySlugBody!.Id);
    }

    [Fact]
    public async Task DeleteSeason_GuestRole_ReturnsForbidden()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Guest);

        HttpResponseMessage response = await client.DeleteAsync($"api/seasons/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed record SeasonBody(Guid Id, string Slug, string Name);
}
