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
    /// Password login for the operator accounts (ADMIN, OWNER).
    /// </summary>
    Task<TokenResponse> LoginAsync(LogInUserRequest request, CancellationToken ct = default);

    /// <summary>
    /// Generates a magic-link token. Deferred to Phase 2 (D6): kept in place
    /// but no longer gated to a specific role after TeamManager was removed.
    /// </summary>
    Task<MagicLinkResponse> RequestMagicLinkAsync(MagicLinkRequest request, CancellationToken ct = default);
    Task<TokenResponse> MagicLinkLoginAsync(MagicLinkLoginRequest request, CancellationToken ct = default);

    /// <summary>
    /// Issues a guest JWT without any database interaction.
    /// </summary>
    Task<TokenResponse> GuestAsync(CancellationToken ct = default);
    Task<TokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);

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
        Guid callerId,
        CancellationToken ct = default);

    /// <summary>
    /// HU-09: creates a user by email only (no password) and emails a magic
    /// activation link so the user sets their own password. Same
    /// role-authorization policy as <see cref="RegisterAsync"/>.
    /// </summary>
    Task<InviteUserResponse> InviteUserAsync(
        InviteUserRequest request,
        string callerRole,
        Guid callerId,
        CancellationToken ct = default);

    /// <summary>
    /// HU-09: consumes the activation token from the invitation email, sets the
    /// user's first password, enables login, and returns a ready-to-use JWT
    /// (the user is logged in immediately after activating).
    /// </summary>
    Task<TokenResponse> ActivateAccountAsync(
        ActivateAccountRequest request, CancellationToken ct = default);

    /// <summary>
    /// HU-10: self-service. Emails a password-reset magic link for the given
    /// email. Completes silently when no account matches (no user enumeration);
    /// the link is consumed by <see cref="ConfirmPasswordResetAsync"/>.
    /// </summary>
    Task RequestPasswordResetAsync(
        RequestPasswordResetRequest request, CancellationToken ct = default);

    /// <summary>
    /// Clears the caller's stored RefreshToken and RefreshTokenExpiryTime.
    /// A no-op if no user matches <paramref name="userId"/>.
    /// </summary>
    Task LogoutAsync(Guid userId, CancellationToken ct = default);
}
