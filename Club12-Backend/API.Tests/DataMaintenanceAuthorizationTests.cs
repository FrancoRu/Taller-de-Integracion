using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using System.Net;

namespace API.Tests;

/// <summary>
/// Proves both data-maintenance endpoints are Admin-only, mirroring the
/// pattern in AuthorizationGatingTests.cs.
/// </summary>
public class DataMaintenanceAuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DataMaintenanceAuthorizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Seed_Anonymous_ReturnsUnauthorized()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync("api/data-maintenance/seed", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Owner)]
    [InlineData(Roles.Guest)]
    public async Task Seed_NonAdminRole_ReturnsForbidden(string role)
    {
        HttpClient client = _factory.CreateAuthenticatedClient(role);

        HttpResponseMessage response = await client.PostAsync("api/data-maintenance/seed", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Seed_AdminRole_Succeeds()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        // Wipe first: this class shares one CustomWebApplicationFactory (and
        // therefore one database) across every [Fact] via IClassFixture, and
        // xUnit does not guarantee execution order — another fact in this
        // class may have already seeded, which would make this seed call
        // return 409 instead of 200.
        await client.PostAsync("api/data-maintenance/wipe", null);

        HttpResponseMessage response = await client.PostAsync("api/data-maintenance/seed", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Wipe_Anonymous_ReturnsUnauthorized()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync("api/data-maintenance/wipe", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Owner)]
    [InlineData(Roles.Guest)]
    public async Task Wipe_NonAdminRole_ReturnsForbidden(string role)
    {
        HttpClient client = _factory.CreateAuthenticatedClient(role);

        HttpResponseMessage response = await client.PostAsync("api/data-maintenance/wipe", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Wipe_AdminRole_Succeeds()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.PostAsync("api/data-maintenance/wipe", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Seed_OnNonEmptyDatabase_ReturnsConflict()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        // Wipe first, same reasoning as Seed_AdminRole_Succeeds above —
        // guarantees the first seed call below actually starts empty
        // regardless of what other facts in this class already did.
        await client.PostAsync("api/data-maintenance/wipe", null);
        await client.PostAsync("api/data-maintenance/seed", null);

        HttpResponseMessage response = await client.PostAsync("api/data-maintenance/seed", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(2, await db.Tournaments.CountAsync());
    }
}
