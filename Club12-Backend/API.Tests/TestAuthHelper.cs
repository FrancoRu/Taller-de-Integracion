using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace API.Tests;

/// <summary>
/// Issues test-only JWTs signed with the same key/issuer/audience
/// CustomWebApplicationFactory configures via environment variables, so
/// integration tests can exercise [Authorize(Roles = ...)]-gated
/// endpoints through a real HTTP round trip instead of hitting them
/// anonymously.
/// </summary>
public static class TestAuthHelper
{
    private const string SigningKey = "test-signing-key-at-least-32-characters-long";
    private const string Issuer = "club12-tests";
    private const string Audience = "club12-tests";

    /// <summary>
    /// Builds a signed JWT for a fake user with the given role(s).
    /// </summary>
    public static string CreateToken(params string[] roles)
    {
        JwtSecurityTokenHandler tokenHandler = new();
        byte[] key = Encoding.UTF8.GetBytes(SigningKey);

        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, "test-user@club12.test"),
            .. roles.Select(role => new Claim(ClaimTypes.Role, role)),
        ];

        SecurityTokenDescriptor tokenDescriptor = new()
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = Issuer,
            Audience = Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
        };

        SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Builds an HttpClient from the factory with a Bearer token for the
    /// given role(s) already attached.
    /// </summary>
    public static HttpClient CreateAuthenticatedClient(this CustomWebApplicationFactory factory, params string[] roles)
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(roles));
        return client;
    }
}
