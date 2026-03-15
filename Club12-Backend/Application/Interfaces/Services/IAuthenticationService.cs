using Application.DTOs.Auth.Request;
using Application.DTOs.Auth.Response;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Application boundary for authentication and user-registration flows.
/// </summary>
/// <remarks>
/// Infrastructure (Identity/EF Core) implements this interface.
/// API controllers depend on this interface to keep Clean Architecture boundaries.
/// </remarks>
public interface IAuthenticationService
{
    Task<TokenResponse>        LoginAsync(LogInUserRequest request, CancellationToken ct = default);
    Task<MagicLinkResponse>    RequestMagicLinkAsync(MagicLinkRequest request, CancellationToken ct = default);
    Task<TokenResponse>        MagicLinkLoginAsync(MagicLinkLoginRequest request, CancellationToken ct = default);
    Task<TokenResponse>        GuestAsync(CancellationToken ct = default);
    Task<TokenResponse>        RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);

    /// <summary>
    /// Registers a new user. The caller's role determines which target roles are permitted.
    /// </summary>
    Task<RegisterUserResponse> RegisterAsync(RegisterUserRequest request, string callerRole, CancellationToken ct = default);
}
