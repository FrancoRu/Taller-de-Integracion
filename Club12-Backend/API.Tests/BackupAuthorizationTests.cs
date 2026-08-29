using Domain.Enums;

using System.Net;

namespace API.Tests;

/// <summary>
/// Proves every api/backups endpoint is staff-only (Admin or Owner),
/// mirroring the pattern in DataMaintenanceAuthorizationTests.cs. Only the
/// negative paths (anonymous/Guest) and the safe read (GET, staff) are
/// exercised via the real HTTP pipeline — POST/DELETE as staff would invoke
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

    [Fact]
    public async Task GetBackups_GuestRole_ReturnsForbidden()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Guest);

        HttpResponseMessage response = await client.GetAsync("api/backups");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.Owner)]
    public async Task GetBackups_StaffRole_Succeeds(string role)
    {
        HttpClient client = _factory.CreateAuthenticatedClient(role);

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

    [Fact]
    public async Task CreateBackup_GuestRole_ReturnsForbidden()
    {
        string role = Roles.Guest;
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

    [Fact]
    public async Task DeleteBackup_GuestRole_ReturnsForbidden()
    {
        string role = Roles.Guest;
        HttpClient client = _factory.CreateAuthenticatedClient(role);

        HttpResponseMessage response = await client.DeleteAsync($"api/backups/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RestoreBackup_Anonymous_ReturnsUnauthorized()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync($"api/backups/{Guid.NewGuid()}/restore", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RestoreBackup_GuestRole_ReturnsForbidden()
    {
        string role = Roles.Guest;
        HttpClient client = _factory.CreateAuthenticatedClient(role);

        HttpResponseMessage response = await client.PostAsync($"api/backups/{Guid.NewGuid()}/restore", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
