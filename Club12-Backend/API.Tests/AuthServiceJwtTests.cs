using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Auth.Response;
using Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace API.Tests;

/// <summary>
/// Characterization tests for <see cref="AuthService.GenerateJwtTokenAsync"/> — locks in the
/// current claim shape, 24h expiry, signature verifiability, and refresh-token uniqueness of
/// the JWT/refresh-token generation behavior before any future refactor.
/// <see cref="AuthService"/> depends only on <see cref="IConfiguration"/>, so it is
/// instantiated directly against an in-memory configuration — no host, no DB.
/// </summary>
public class AuthServiceJwtTests
{
    private const string SigningKey = "unit-test-signing-key-at-least-32-characters-long";
    private const string Issuer = "club12-unit-tests";
    private const string Audience = "club12-unit-tests";

    [Fact]
    public async Task GenerateJwtTokenAsync_IncludesUserIdAndRoleClaims()
    {
        AuthService authService = new(BuildConfig());
        Guid userId = Guid.NewGuid();
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, "ADMIN"),
        ];

        TokenResponse response = await authService.GenerateJwtTokenAsync(claims);
        ClaimsPrincipal principal = ValidateToken(response.AccessToken);

        Assert.Equal(userId.ToString(), principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.True(principal.IsInRole("ADMIN"));
    }

    [Fact]
    public async Task GenerateJwtTokenAsync_ExpiresApproximately24HoursFromIssuance()
    {
        AuthService authService = new(BuildConfig());
        Claim[] claims = [new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())];

        DateTime beforeIssuance = DateTime.UtcNow;
        TokenResponse response = await authService.GenerateJwtTokenAsync(claims);

        JwtSecurityTokenHandler handler = new();
        JwtSecurityToken token = handler.ReadJwtToken(response.AccessToken);

        DateTime expectedExpiry = beforeIssuance.AddHours(24);
        Assert.True(
            Math.Abs((token.ValidTo - expectedExpiry).TotalMinutes) < 2,
            $"Expected expiry near {expectedExpiry:o}, got {token.ValidTo:o}");
        Assert.Equal(TimeSpan.FromHours(24), response.ExpiresIn);
    }

    [Fact]
    public async Task GenerateJwtTokenAsync_AccessTokenValidatesAgainstConfiguredKeyIssuerAudience()
    {
        AuthService authService = new(BuildConfig());
        Claim[] claims = [new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())];

        TokenResponse response = await authService.GenerateJwtTokenAsync(claims);

        JwtSecurityTokenHandler handler = new();
        ClaimsPrincipal principal = handler.ValidateToken(
            response.AccessToken, BuildValidationParameters(), out SecurityToken validatedToken);

        Assert.NotNull(principal);
        JwtSecurityToken jwt = Assert.IsType<JwtSecurityToken>(validatedToken);
        Assert.Equal(Issuer, jwt.Issuer);
        Assert.Contains(Audience, jwt.Audiences);
    }

    [Fact]
    public async Task GenerateJwtTokenAsync_TwoCallsYieldDifferentRefreshTokens()
    {
        AuthService authService = new(BuildConfig());
        Claim[] claims = [new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())];

        TokenResponse first = await authService.GenerateJwtTokenAsync(claims);
        TokenResponse second = await authService.GenerateJwtTokenAsync(claims);

        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
    }

    private static ClaimsPrincipal ValidateToken(string accessToken)
    {
        JwtSecurityTokenHandler handler = new();
        return handler.ValidateToken(accessToken, BuildValidationParameters(), out _);
    }

    private static TokenValidationParameters BuildValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = Issuer,
        ValidateAudience = true,
        ValidAudience = Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
    };

    private static IConfiguration BuildConfig()
    {
        Dictionary<string, string?> settings = new()
        {
            ["JWT:Key"] = SigningKey,
            ["JWT:Issuer"] = Issuer,
            ["JWT:Audience"] = Audience,
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }
}
