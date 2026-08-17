using Application.DTOs.Auth.Request;
using Application.DTOs.Auth.Response;
using Application.Interfaces.Services;
using Application.Utils.Constants;
using Application.Utils.Constants.Auth;
using Application.Utils.Constants.Configuration;

using Domain.Enums;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Identity;

/// <summary>
/// Infrastructure implementation of IAuthenticationService backed by ASP.NET Core Identity.
/// </summary>
public sealed class IdentityAuthenticationService(
    UserManager<ApplicationUser> userManager,
    IAuthService authService,
    IEmailService emailService,
    IConfiguration configuration) : IAuthenticationService
{
    private const string MagicLinkPurpose = "MagicLink";
    private const int RefreshExpiryDays = 7;

    private static readonly Dictionary<string, HashSet<string>> _creationPolicy =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [UserRoleType.ADMIN.ToRoleName()] = new(StringComparer.OrdinalIgnoreCase)
            {
                UserRoleType.ADMIN.ToRoleName(),
                UserRoleType.OWNER.ToRoleName(),
                UserRoleType.TOURNAMENT_MANAGER.ToRoleName(),
                UserRoleType.TEAM_MANAGER.ToRoleName()
            },
            [UserRoleType.OWNER.ToRoleName()] = new(StringComparer.OrdinalIgnoreCase)
            {
                UserRoleType.ADMIN.ToRoleName(),
                UserRoleType.OWNER.ToRoleName(),
                UserRoleType.TOURNAMENT_MANAGER.ToRoleName(),
                UserRoleType.TEAM_MANAGER.ToRoleName()
            }
        };

    /// <inheritdoc/>
    public async Task<RegisterUserResponse> RegisterAsync(
        RegisterUserRequest request, string callerRole, Guid callerId, CancellationToken ct = default)
    {
        EnforceCreationPolicy(callerRole, request.Role);

        ApplicationUser? existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            throw new InvalidOperationException(ErrorMessages.Auth.EmailAlreadyExists);
        }

        ApplicationUser user = new()
        {
            UserName = request.Username,
            Email = request.Email,
            EmailConfirmed = true,
            PhoneNumber = request.Phone,
            CreatedByOwnerId = IsOwner(callerRole) ? callerId : null,
            MustChangePassword = true
        };

        string temporaryPassword = GenerateTemporaryPassword();

        IdentityResult result = await userManager.CreateAsync(user, temporaryPassword);

        if (!result.Succeeded)
        {
            string errors = string.Join(" | ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException(ErrorMessages.Auth.UserCreationFailed(errors));
        }

        await userManager.AddToRoleAsync(user, request.Role.ToUpperInvariant());

        string token = await userManager.GeneratePasswordResetTokenAsync(user);

        string frontendUrl = configuration[ConfigurationKeys.Frontend.PasswordResetUrl]
            ?? throw new InvalidOperationException(
                ErrorMessages.Configuration.KeyNotConfigured(ConfigurationKeys.Frontend.PasswordResetUrl));

        string setPasswordLink =
            $"{frontendUrl}" +
            $"?email={Uri.EscapeDataString(user.Email!)}" +
            $"&token={Uri.EscapeDataString(token)}";

        await emailService.SendWelcomeSetPasswordAsync(user.Email!, user.UserName!, setPasswordLink, ct);

        return new RegisterUserResponse(
            user.Id, user.Email!, user.UserName!, request.Role.ToUpperInvariant(), user.PhoneNumber);
    }

    /// <inheritdoc/>
    public async Task<TokenResponse> LoginAsync(LogInUserRequest request, CancellationToken ct = default)
    {
        ApplicationUser user = await userManager.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException(ErrorMessages.Auth.InvalidCredentials);

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedAccessException(ErrorMessages.Auth.InvalidCredentials);
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            throw new UnauthorizedAccessException(ErrorMessages.Auth.AccountDeactivated);
        }

        IList<string> roles = await userManager.GetRolesAsync(user);

        return roles.Contains(UserRoleType.TEAM_MANAGER.ToRoleName())
            ? throw new UnauthorizedAccessException(ErrorMessages.Auth.TeamManagerMustUseMagicLink)
            : await BuildTokenResponseAsync(user, roles, ct);
    }

    /// <inheritdoc/>
    public async Task<MagicLinkResponse> RequestMagicLinkAsync(
        MagicLinkRequest request, CancellationToken ct = default)
    {
        ApplicationUser user = await userManager.FindByEmailAsync(request.Email)
            ?? throw new KeyNotFoundException(ErrorMessages.Auth.NoAccountForEmail);

        IList<string> roles = await userManager.GetRolesAsync(user);

        if (!roles.Contains(UserRoleType.TEAM_MANAGER.ToRoleName()))
        {
            throw new UnauthorizedAccessException(ErrorMessages.Auth.MagicLinkOnlyForTeamManager);
        }

        string token = await userManager.GenerateUserTokenAsync(
            user, TokenOptions.DefaultEmailProvider, MagicLinkPurpose);

        string magicLink =
            $"/api/auth/magic-link/login" +
            $"?email={Uri.EscapeDataString(user.Email!)}" +
            $"&token={Uri.EscapeDataString(token)}";

        return new MagicLinkResponse("Magic link generated. Check your email.", magicLink);
    }

    /// <inheritdoc/>
    public async Task<TokenResponse> MagicLinkLoginAsync(
        MagicLinkLoginRequest request, CancellationToken ct = default)
    {
        ApplicationUser user = await userManager.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException(ErrorMessages.Auth.InvalidMagicLink);

        bool valid = await userManager.VerifyUserTokenAsync(
            user, TokenOptions.DefaultEmailProvider, MagicLinkPurpose, request.Token);

        if (!valid)
        {
            throw new UnauthorizedAccessException(ErrorMessages.Auth.MagicLinkAlreadyUsed);
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            throw new UnauthorizedAccessException(ErrorMessages.Auth.AccountDeactivated);
        }

        IList<string> roles = await userManager.GetRolesAsync(user);
        return await BuildTokenResponseAsync(user, roles, ct);
    }

    /// <inheritdoc/>
    public async Task<TokenResponse> ConfirmPasswordResetAsync(
        PasswordResetConfirmRequest request, CancellationToken ct = default)
    {
        ApplicationUser user = await userManager.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException(ErrorMessages.Auth.InvalidPasswordResetRequest);

        IdentityResult result = await userManager.ResetPasswordAsync(
            user, request.Token, request.NewPassword);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        user.MustChangePassword = false;
        await userManager.UpdateAsync(user);

        IList<string> roles = await userManager.GetRolesAsync(user);
        return await BuildTokenResponseAsync(user, roles, ct);
    }

    /// <inheritdoc/>
    public async Task<TokenResponse> GuestAsync(CancellationToken ct = default)
    {
        Claim[] claims = [new(ClaimTypes.Role, UserRoleType.GUEST.ToRoleName())];
        TokenResponse response = await authService.GenerateJwtTokenAsync(claims, ct);
        return new TokenResponse(response.AccessToken, response.ExpiresIn, refreshToken: null);
    }

    /// <inheritdoc/>
    public async Task<TokenResponse> RefreshAsync(
        RefreshTokenRequest request, CancellationToken ct = default)
    {
        ApplicationUser user = await userManager.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, ct)
            ?? throw new UnauthorizedAccessException(ErrorMessages.Auth.InvalidRefreshToken);

        if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException(ErrorMessages.Auth.RefreshTokenExpired);
        }

        IList<string> roles = await userManager.GetRolesAsync(user);
        return await BuildTokenResponseAsync(user, roles, ct);
    }

    /// <inheritdoc/>
    public async Task LogoutAsync(Guid userId, CancellationToken ct = default)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId.ToString());
        if (user is not null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await userManager.UpdateAsync(user);
        }
    }

    private async Task<TokenResponse> BuildTokenResponseAsync(
        ApplicationUser user, IList<string> roles, CancellationToken ct)
    {
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            .. roles.Select(r => new Claim(ClaimTypes.Role, r))
        ];

        if (user.MustChangePassword)
        {
            claims.Add(new Claim(CustomClaimTypes.MustChangePassword, "true"));
        }

        TokenResponse response = await authService.GenerateJwtTokenAsync(claims, ct);

        user.RefreshToken = response.RefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(RefreshExpiryDays);
        await userManager.UpdateAsync(user);

        return response;
    }

    private static string GenerateTemporaryPassword(int length = 16)
    {
        if (length < 8)
        {
            length = 8;
        }

        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%^&*_-+=?";
        string all = upper + lower + digits + special;

        char[] chars = new char[length];
        chars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        chars[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        chars[3] = special[RandomNumberGenerator.GetInt32(special.Length)];

        for (int i = 4; i < chars.Length; i++)
        {
            chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        }

        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }

    private static void EnforceCreationPolicy(string callerRole, string targetRole)
    {
        bool allowed = _creationPolicy.TryGetValue(callerRole, out HashSet<string>? permitted)
                       && permitted.Contains(targetRole);

        if (!allowed)
        {
            throw new UnauthorizedAccessException(ErrorMessages.Auth.RoleNotAllowedToCreate(callerRole, targetRole));
        }
    }

    private static bool IsOwner(string role)
    {
        return role.Equals(UserRoleType.OWNER.ToRoleName(), StringComparison.OrdinalIgnoreCase);
    }
}
