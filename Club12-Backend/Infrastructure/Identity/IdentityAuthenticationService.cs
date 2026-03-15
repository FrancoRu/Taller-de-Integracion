using Application.DTOs.Auth.Request;
using Application.DTOs.Auth.Response;
using Application.Interfaces.Services;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Identity;

/// <summary>
/// Infrastructure implementation of <see cref="IAuthenticationService"/> backed by ASP.NET Core Identity.
/// <list type="bullet">
///   <item>ADMIN, OWNER, TOURNAMENT_MANAGER → password login.</item>
///   <item>TEAM_MANAGER → magic-link flow only.</item>
///   <item>GUEST → anonymous JWT, no database interaction.</item>
/// </list>
/// JWT generation is delegated to <see cref="IAuthService"/> (Application layer),
/// keeping this class focused on Identity orchestration only.
/// </summary>
public sealed class IdentityAuthenticationService(
    UserManager<ApplicationUser> userManager,
    IAuthService                 authService) : IAuthenticationService
{
    private const string MagicLinkPurpose    = "MagicLink";
    private const int    RefreshExpiryDays   = 7;

    // ─────────────────────────────────────────────────────────────
    // Role-creation permission matrix
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Maps each caller role to the set of roles it is allowed to create.
    /// Roles absent from this map cannot create any user.
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> _creationPolicy =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [UserRoleType.ADMIN.ToRoleName()] = new(StringComparer.OrdinalIgnoreCase)
            {
                UserRoleType.ADMIN.ToRoleName(),
                UserRoleType.OWNER.ToRoleName(),
                UserRoleType.TOURNAMENT_MANAGER.ToRoleName(),
                UserRoleType.TEAM_MANAGER.ToRoleName()   // Admin must be able to create TeamManagers
            },
            [UserRoleType.OWNER.ToRoleName()] = new(StringComparer.OrdinalIgnoreCase)
            {
                UserRoleType.TOURNAMENT_MANAGER.ToRoleName()
            }
        };

    // ─────────────────────────────────────────────────────────────
    // Register
    // ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<RegisterUserResponse> RegisterAsync(
        RegisterUserRequest request, string callerRole, CancellationToken ct = default)
    {
        EnforceCreationPolicy(callerRole, request.Role);

        ApplicationUser user = new()
        {
            UserName       = request.Email,
            Email          = request.Email,
            EmailConfirmed = true,
        };

        bool isTeamManager = string.Equals(
            request.Role, UserRoleType.TEAM_MANAGER.ToRoleName(),
            StringComparison.OrdinalIgnoreCase);

        IdentityResult result = isTeamManager
            ? await userManager.CreateAsync(user)                  // No password — magic-link only
            : await CreateWithPasswordAsync(user, request.Password);

        if (!result.Succeeded)
        {
            string errors = string.Join(" | ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"User creation failed: {errors}");
        }

        await userManager.AddToRoleAsync(user, request.Role.ToUpperInvariant());

        return new RegisterUserResponse(user.Id, user.Email!, request.Role.ToUpperInvariant());
    }

    // ─────────────────────────────────────────────────────────────
    // Password login  (ADMIN | OWNER | TOURNAMENT_MANAGER)
    // ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<TokenResponse> LoginAsync(LogInUserRequest request, CancellationToken ct = default)
    {
        ApplicationUser user = await userManager.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!await userManager.CheckPasswordAsync(user, request.Password))
            throw new UnauthorizedAccessException("Invalid credentials.");

        IList<string> roles = await userManager.GetRolesAsync(user);

        if (roles.Contains(UserRoleType.TEAM_MANAGER.ToRoleName()))
            throw new UnauthorizedAccessException("TeamManager accounts must authenticate via the magic-link flow.");

        return await BuildTokenResponseAsync(user, roles, ct);
    }

    // ─────────────────────────────────────────────────────────────
    // Magic-link  (TEAM_MANAGER only)
    // ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<MagicLinkResponse> RequestMagicLinkAsync(MagicLinkRequest request, CancellationToken ct = default)
    {
        ApplicationUser user = await userManager.FindByEmailAsync(request.Email)
            ?? throw new KeyNotFoundException("No account found for that email.");

        IList<string> roles = await userManager.GetRolesAsync(user);

        if (!roles.Contains(UserRoleType.TEAM_MANAGER.ToRoleName()))
            throw new UnauthorizedAccessException("Magic-link is only available for TeamManager accounts.");

        string token = await userManager.GenerateUserTokenAsync(
            user, TokenOptions.DefaultEmailProvider, MagicLinkPurpose);

        // TODO: dispatch via email — never expose the token in production responses.
        string magicLink =
            $"/api/auth/magic-link/login" +
            $"?email={Uri.EscapeDataString(user.Email!)}" +
            $"&token={Uri.EscapeDataString(token)}";

        return new MagicLinkResponse("Magic link generated. Check your email.", magicLink);
    }

    /// <inheritdoc/>
    public async Task<TokenResponse> MagicLinkLoginAsync(MagicLinkLoginRequest request, CancellationToken ct = default)
    {
        ApplicationUser user = await userManager.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid magic-link.");

        bool valid = await userManager.VerifyUserTokenAsync(
            user, TokenOptions.DefaultEmailProvider, MagicLinkPurpose, request.Token);

        if (!valid)
            throw new UnauthorizedAccessException("Magic-link is invalid or has already been used.");

        IList<string> roles = await userManager.GetRolesAsync(user);
        return await BuildTokenResponseAsync(user, roles, ct);
    }

    // ─────────────────────────────────────────────────────────────
    // Guest  (no DB interaction)
    // ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<TokenResponse> GuestAsync(CancellationToken ct = default)
    {
        Claim[] claims = [new(ClaimTypes.Role, UserRoleType.GUEST.ToRoleName())];
        TokenResponse response = await authService.GenerateJwtTokenAsync(claims, ct);

        return new TokenResponse(response.AccessToken, response.ExpiresIn, refreshToken: null);
    }

    // ─────────────────────────────────────────────────────────────
    // Refresh token
    // ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<TokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        ApplicationUser user = await userManager.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, ct)
            ?? throw new UnauthorizedAccessException("Refresh token is invalid.");

        if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token has expired. Please log in again.");

        IList<string> roles = await userManager.GetRolesAsync(user);
        return await BuildTokenResponseAsync(user, roles, ct);
    }

    // ─────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────

    private async Task<TokenResponse> BuildTokenResponseAsync(
        ApplicationUser user, IList<string> roles, CancellationToken ct)
    {
        IEnumerable<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email,          user.Email!),
            ..roles.Select(r => new Claim(ClaimTypes.Role, r))
        ];

        TokenResponse response = await authService.GenerateJwtTokenAsync(claims, ct);

        user.RefreshToken           = response.RefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(RefreshExpiryDays);
        await userManager.UpdateAsync(user);

        return response;
    }

    private async Task<IdentityResult> CreateWithPasswordAsync(ApplicationUser user, string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is required for this role.");

        return await userManager.CreateAsync(user, password);
    }

    /// <summary>
    /// Throws <see cref="UnauthorizedAccessException"/> if <paramref name="callerRole"/>
    /// is not allowed to create a user with <paramref name="targetRole"/>.
    /// </summary>
    private static void EnforceCreationPolicy(string callerRole, string targetRole)
    {
        bool allowed = _creationPolicy.TryGetValue(callerRole, out HashSet<string>? permitted)
                       && permitted.Contains(targetRole);

        if (!allowed)
            throw new UnauthorizedAccessException(
                $"Role '{callerRole}' is not allowed to create users with role '{targetRole}'.");
    }
}
