using Application.DTOs.Auth.Response;

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Token generator service for JWT and refresh tokens.
/// </summary>
public interface IAuthService
{
    Task<TokenResponse> GenerateJwtTokenAsync(IEnumerable<Claim> claims, CancellationToken ct = default);
}
