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
    /// <summary>
    /// Password login for the operator accounts, ADMIN and OWNER.
    /// </summary>
    Task<TokenResponse> LoginAsync(LogInUserRequest request, CancellationToken ct = default);

    /// <summary>
    /// Generates a magic-link token, deferred to Phase 2 D6.
    /// </summary>
    Task<MagicLinkResponse> RequestMagicLinkAsync(MagicLinkRequest request, CancellationToken ct = default);
    Task<TokenResponse> MagicLinkLoginAsync(MagicLinkLoginRequest request, CancellationToken ct = default);

    /// <summary>
    /// Issues a guest JWT without any database interaction.
    /// </summary>
    Task<TokenResponse> GuestAsync(CancellationToken ct = default);
    Task<TokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);

    /// <summary>
    /// Verifies the password-reset token from the email link, sets the new password, clears MustChangePassword, and returns a ready-to-use JWT.
    /// </summary>
    Task<TokenResponse> ConfirmPasswordResetAsync(
        PasswordResetConfirmRequest request, CancellationToken ct = default);

    /// <summary>
    /// Registers a new user, where callerRole determines permitted target roles.
    /// </summary>
    Task<RegisterUserResponse> RegisterAsync(
        RegisterUserRequest request,
        string callerRole,
        Guid callerId,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a user by email only, with no password, and emails a magic activation link so the user sets their own password.
    /// </summary>
    Task<InviteUserResponse> InviteUserAsync(
        InviteUserRequest request,
        string callerRole,
        Guid callerId,
        CancellationToken ct = default);

    /// <summary>
    /// Consumes the activation token from the invitation email, sets the user's first password, enables login, and returns a ready-to-use JWT.
    /// </summary>
    Task<TokenResponse> ActivateAccountAsync(
        ActivateAccountRequest request, CancellationToken ct = default);

    /// <summary>
    /// Emails a password-reset magic link for the given email, completing silently with no user enumeration when no account matches.
    /// </summary>
    Task RequestPasswordResetAsync(
        RequestPasswordResetRequest request, CancellationToken ct = default);

    /// <summary>
    /// Clears the caller's stored RefreshToken and RefreshTokenExpiryTime.
    /// </summary>
    Task LogoutAsync(Guid userId, CancellationToken ct = default);
}
