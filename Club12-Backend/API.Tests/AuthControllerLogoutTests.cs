using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.DTOs.Auth.Response;
using Application.Interfaces.Services;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.Tests;

/// <summary>
/// Regression test locking AuthController.Logout's observable contract — 204 No
/// Content and refresh-token/expiry clearing for an existing user, no-op for a missing
/// user — before the boundary fix that routes the side effect through the new
/// IAuthenticationService.LogoutAsync instead of a direct
/// UserManager&lt;ApplicationUser&gt; injection in the controller. Written first
/// (RED) against the pre-refactor controller so it proves behavior is unchanged (GREEN)
/// after the refactor lands.
/// </summary>
public class AuthControllerLogoutTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthControllerLogoutTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Verifies token clearing through a fresh scope/DbContext: the request
    /// pipeline persists through its own scoped DbContext, so re-querying with
    /// a new one avoids reading a stale change-tracker snapshot instead of the
    /// actual persisted row.
    /// </summary>
    [Fact]
    public async Task Logout_ExistingUserWithRefreshToken_Returns204AndClearsToken()
    {
        ApplicationUser user;
        using (IServiceScope seedScope = _factory.Services.CreateScope())
        {
            UserManager<ApplicationUser> seedUserManager =
                seedScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            user = await SeedUserWithRefreshTokenAsync(seedUserManager);
        }

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await CreateAccessTokenAsync(user.Id));

        HttpResponseMessage response = await client.PostAsync("api/auth/logout", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using IServiceScope verifyScope = _factory.Services.CreateScope();
        UserManager<ApplicationUser> verifyUserManager =
            verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        ApplicationUser? persisted = await verifyUserManager.FindByIdAsync(user.Id.ToString());
        Assert.NotNull(persisted);
        Assert.Null(persisted!.RefreshToken);
        Assert.Null(persisted.RefreshTokenExpiryTime);
    }

    [Fact]
    public async Task Logout_MissingUser_Returns204AndNoOp()
    {
        Guid missingUserId = Guid.NewGuid();

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await CreateAccessTokenAsync(missingUserId));

        HttpResponseMessage response = await client.PostAsync("api/auth/logout", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static async Task<ApplicationUser> SeedUserWithRefreshTokenAsync(
        UserManager<ApplicationUser> userManager)
    {
        string uniqueEmail = $"logout-test-{Guid.NewGuid()}@test.local";

        ApplicationUser user = new()
        {
            UserName = uniqueEmail,
            Email = uniqueEmail,
            EmailConfirmed = true,
            RefreshToken = Guid.NewGuid().ToString("N"),
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7),
        };

        IdentityResult result = await userManager.CreateAsync(user, "Test-Passw0rd!1");
        Assert.True(result.Succeeded, string.Join(" | ", result.Errors.Select(e => e.Description)));

        return user;
    }

    private async Task<string> CreateAccessTokenAsync(Guid userId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IAuthService authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, "ADMIN"),
        ];

        TokenResponse token = await authService.GenerateJwtTokenAsync(claims);
        return token.AccessToken;
    }
}
