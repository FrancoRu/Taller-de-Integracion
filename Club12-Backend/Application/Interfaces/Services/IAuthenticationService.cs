using Application.DTOs.Auth.Request;
using Application.DTOs.Auth.Response;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Application boundary for authentication and user-registration flows.
/// </summary>
public interface IAuthenticationService
{
    Task<TokenResponse>        LoginAsync(LogInUserRequest request, CancellationToken ct = default);
    Task<MagicLinkResponse>    RequestMagicLinkAsync(MagicLinkRequest request, CancellationToken ct = default);
    Task<TokenResponse>        MagicLinkLoginAsync(MagicLinkLoginRequest request, CancellationToken ct = default);
    Task<TokenResponse>        GuestAsync(CancellationToken ct = default);
    Task<TokenResponse>        RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);

    /// <summary>
    /// Verifies the password-reset token from the email link, sets the new password,
    /// clears MustChangePassword, and returns a ready-to-use JWT.
    /// </summary>
    Task<TokenResponse> ConfirmPasswordResetAsync(
        PasswordResetConfirmRequest request, CancellationToken ct = default);

    /// <summary>
    /// Registers a new user. <paramref name="callerRole"/> determines permitted target roles;
    /// <paramref name="callerId"/> is stored as CreatedByOwnerId when the caller is an OWNER.
    /// </summary>
    Task<RegisterUserResponse> RegisterAsync(
        RegisterUserRequest request,
        string callerRole,
        Guid   callerId,
        CancellationToken ct = default);

    /// <summary>
    /// Clears the caller's stored RefreshToken and RefreshTokenExpiryTime.
    /// A no-op if no user matches <paramref name="userId"/>.
    /// </summary>
    Task LogoutAsync(Guid userId, CancellationToken ct = default);
}
