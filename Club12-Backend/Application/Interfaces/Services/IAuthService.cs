using Application.DTOs.Auth.Response;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Token generator service (JWT + refresh token).
/// </summary>
/// <remarks>
/// This service is framework-agnostic (no Identity types). It only needs a set of claims.
/// </remarks>
public interface IAuthService
{
    Task<TokenResponse> GenerateJwtTokenAsync(IEnumerable<Claim> claims, CancellationToken ct = default);
}
