using Application.DTOs.Tournament.Request;

using Domain.Enums;

using Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using System.Net;
using System.Net.Http.Json;

namespace API.Tests;

/// <summary>
/// HU-05: the role model is reduced to the two operator accounts (Owner,
/// Admin IT) plus the technical Guest role. These tests prove the removed
/// TournamentManager / TeamManager roles are gone from seeding and no longer
/// authorize a staff-only endpoint, while Owner and Admin still do.
/// </summary>
public class RoleSeedingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RoleSeedingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SeededRoles_AreExactlyOwnerAdminAndGuest()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IdentityAppDbContext db = scope.ServiceProvider.GetRequiredService<IdentityAppDbContext>();

        List<string> roleNames = await db.Roles
            .Select(r => r.Name!)
            .ToListAsync();

        Assert.Contains(Roles.Admin, roleNames);
        Assert.Contains(Roles.Owner, roleNames);
        Assert.Contains(Roles.Guest, roleNames);

        Assert.DoesNotContain("TOURNAMENT_MANAGER", roleNames);
        Assert.DoesNotContain("TEAM_MANAGER", roleNames);
        Assert.Equal(3, roleNames.Count);
    }

    [Theory]
    [InlineData("TOURNAMENT_MANAGER")]
    [InlineData("TEAM_MANAGER")]
    public async Task RemovedRole_DoesNotAuthorizeStaffEndpoint(string removedRole)
    {
        HttpClient client = _factory.CreateAuthenticatedClient(removedRole);

        HttpResponseMessage response = await client.PostAsJsonAsync("api/tournaments", BuildTournamentRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Owner)]
    [InlineData(Roles.Admin)]
    public async Task OwnerAndAdmin_AreAuthorizedOnStaffEndpoint(string staffRole)
    {
        HttpClient client = _factory.CreateAuthenticatedClient(staffRole);

        HttpResponseMessage response = await client.PostAsJsonAsync("api/tournaments", BuildTournamentRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static CreateTournamentRequest BuildTournamentRequest()
    {
        DateTime start = DateTime.UtcNow.Date.AddDays(30);

        return new CreateTournamentRequest
        {
            Name = $"Tournament-{Guid.NewGuid()}",
            Description = "Role seeding test tournament",
            StartDate = start,
            TeamRegistrationDeadline = start.AddDays(-1),
        };
    }
}
