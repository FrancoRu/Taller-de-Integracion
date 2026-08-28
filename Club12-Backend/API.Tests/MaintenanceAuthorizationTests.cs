using Application.Interfaces.Backup;

using Domain.Enums;

using Microsoft.Extensions.DependencyInjection;

using System.Net;

namespace API.Tests;

/// <summary>
/// Proves every api/maintenance endpoint is Admin-only, mirroring the
/// pattern in BackupAuthorizationTests.cs and
/// DataMaintenanceAuthorizationTests.cs. The DELETE (escape-hatch)
/// tests explicitly put the shared IMaintenanceModeState into an
/// active window first — threat-matrix "Escape-hatch abuse": the
/// middleware allow-lists this path so the request reaches MVC, but
/// [Authorize(Roles = Admin)] must still reject anonymous/non-Admin
/// callers even while maintenance is active.
/// </summary>
public class MaintenanceAuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MaintenanceAuthorizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetStatus_Anonymous_ReturnsUnauthorized()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("api/maintenance");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Owner)]
    [InlineData(Roles.Guest)]
    public async Task GetStatus_NonAdminRole_ReturnsForbidden(string role)
    {
        HttpClient client = _factory.CreateAuthenticatedClient(role);

        HttpResponseMessage response = await client.GetAsync("api/maintenance");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetStatus_AdminRole_Succeeds()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.GetAsync("api/maintenance");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ExitMaintenance_AnonymousWhileActive_ReturnsUnauthorized()
    {
        IMaintenanceModeState state = _factory.Services.GetRequiredService<IMaintenanceModeState>();
        state.Enter("test: restore in progress");
        try
        {
            HttpClient client = _factory.CreateClient();

            HttpResponseMessage response = await client.DeleteAsync("api/maintenance");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            state.Exit();
        }
    }

    [Theory]
    [InlineData(Roles.Owner)]
    [InlineData(Roles.Guest)]
    public async Task ExitMaintenance_NonAdminRoleWhileActive_ReturnsForbidden(string role)
    {
        IMaintenanceModeState state = _factory.Services.GetRequiredService<IMaintenanceModeState>();
        state.Enter("test: restore in progress");
        try
        {
            HttpClient client = _factory.CreateAuthenticatedClient(role);

            HttpResponseMessage response = await client.DeleteAsync("api/maintenance");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        finally
        {
            state.Exit();
        }
    }

    [Fact]
    public async Task ExitMaintenance_AdminRoleWhileActive_Succeeds_AndClearsState()
    {
        IMaintenanceModeState state = _factory.Services.GetRequiredService<IMaintenanceModeState>();
        state.Enter("test: restore in progress");
        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        HttpResponseMessage response = await client.DeleteAsync("api/maintenance");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(state.IsActive);
    }
}
