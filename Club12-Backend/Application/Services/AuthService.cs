using Application.DTOs.Auth.Response;
using Application.Interfaces.Services;
using Application.Utils.Constants.Auth;
using Application.Utils.Constants.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// Generates JWT access tokens and refresh tokens.
/// </summary>
public class AuthService(IConfiguration configuration) : IAuthService
{
    private readonly string _jwtSecret = configuration.GetSection(ConfigurationKeys.Jwt.Key).Value
        ?? throw new ArgumentNullException(nameof(configuration), "The JWT Key should be initialized");
    private readonly string _issuer = configuration.GetSection(ConfigurationKeys.Jwt.Issuer).Value
        ?? throw new ArgumentNullException(nameof(configuration), "The JWT Issuer should be initialized");
    private readonly string _audience = configuration.GetSection(ConfigurationKeys.Jwt.Audience).Value
        ?? throw new ArgumentNullException(nameof(configuration), "The JWT Audience should be initialized");

    public Task<TokenResponse> GenerateJwtTokenAsync(IEnumerable<Claim> claims, CancellationToken ct = default)
    {
        JwtSecurityTokenHandler tokenHandler = new();
        byte[] key = Encoding.UTF8.GetBytes(_jwtSecret);

        SecurityTokenDescriptor tokenDescriptor = new()
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(TokenLifetime.AccessTokenExpiryHours),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
        string accessToken = tokenHandler.WriteToken(token);

        string refreshToken = GenerateRefreshToken();
        return Task.FromResult(new TokenResponse(
            accessToken, TimeSpan.FromHours(TokenLifetime.AccessTokenExpiryHours), refreshToken));
    }

    private static string GenerateRefreshToken()
    {
        byte[] randomBytes = new byte[TokenLifetime.RefreshTokenByteLength];
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}