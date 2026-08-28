using Domain.Enums;

using System.Net;
using System.Net.Http.Json;

namespace API.Tests;

/// <summary>
/// Proves the HU-91/93 backup endpoints are Admin/Owner-only and that the
/// HU-92 maintenance status endpoint is anonymously reachable (so the banner
/// works for everyone). Only checks authorization boundaries — it never
/// triggers a real backup/restore (that needs pg_dump/psql), so no dump is
/// executed here.
/// </summary>
public class BackupAuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public BackupAuthorizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
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
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Guest);

        HttpResponseMessage response = await client.PostAsync("api/backups", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListBackups_Anonymous_ReturnsUnauthorized()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("api/backups");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Restore_GuestRole_ReturnsForbidden()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Guest);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "api/backups/restore", new { backupName = "whatever.sql" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Status_Anonymous_ReturnsOk()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("api/backups/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
