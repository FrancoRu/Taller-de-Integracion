using Domain.Enums;

using System.Net;

namespace API.Tests;

/// <summary>
/// Proves every api/backups endpoint is Admin-only, mirroring the
/// pattern in DataMaintenanceAuthorizationTests.cs. Only the
/// negative paths (anonymous/non-Admin) and the safe read (GET, Admin) are
/// exercised via the real HTTP pipeline — POST/DELETE as Admin would invoke
/// the real BackupOperationsService (pg_dump), which has no
/// binary available in this test environment; that outcome-mapping behavior
/// is covered instead by the pure unit tests in
/// Backup/BackupControllerTests.cs.
/// </summary>
public class BackupAuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public BackupAuthorizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetBackups_Anonymous_ReturnsUnauthorized()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("api/backups");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Owner)]
    [InlineData(Roles.TournamentManager)]
    [InlineData(Roles.TeamManager)]
    [InlineData(Roles.Guest)]
    public async Task GetBackups_NonAdminRole_ReturnsForbidden(string role)
    {
        HttpClient client = _factory.CreateAuthenticatedClient(role);

        HttpResponseMessage response = await client.GetAsync("api/backups");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetBackups_AdminRole_Succeeds()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.GetAsync("api/backups");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateBackup_Anonymous_ReturnsUnauthorized()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync("api/backups", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Owner)]
    [InlineData(Roles.TournamentManager)]
    [InlineData(Roles.TeamManager)]
    [InlineData(Roles.Guest)]
    public async Task CreateBackup_NonAdminRole_ReturnsForbidden(string role)
    {
        HttpClient client = _factory.CreateAuthenticatedClient(role);

        HttpResponseMessage response = await client.PostAsync("api/backups", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBackup_Anonymous_ReturnsUnauthorized()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.DeleteAsync($"api/backups/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Owner)]
    [InlineData(Roles.TournamentManager)]
    [InlineData(Roles.TeamManager)]
    [InlineData(Roles.Guest)]
    public async Task DeleteBackup_NonAdminRole_ReturnsForbidden(string role)
    {
        HttpClient client = _factory.CreateAuthenticatedClient(role);

        HttpResponseMessage response = await client.DeleteAsync($"api/backups/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
