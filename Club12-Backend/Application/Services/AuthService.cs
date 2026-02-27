using Application.DTOs.User.Response;
using Application.Interfaces.Services;
using Domain.Entities.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// Provides authentication services, including JWT token generation and refresh token creation.
/// </summary>
/// <remarks>
/// <para>
/// The <c>AuthService</c> class implements <see cref="IAuthService"/> and is responsible for generating and refreshing JWT tokens for user authentication.
/// It uses configuration values for the JWT secret, issuer, and audience, which are required for token signing and validation.
/// </para>
/// <para>
/// The service exposes methods to:
/// <list type="bullet">
///   <item><description>Generate a JWT access token and a secure refresh token for a given user.</description></item>
///   <item><description>Refresh the JWT token using the user entity.</description></item>
/// </list>
/// </para>
/// <para>
/// The JWT token includes claims for username, role, and user ID, and is signed using HMAC SHA256.
/// The refresh token is generated using a cryptographically secure random number generator.
/// </para>
/// </remarks>
public class AuthService(IConfiguration configuration) : IAuthService
{
    private readonly string _jwtSecret = configuration.GetSection("JWT:Key").Value ?? throw new ArgumentNullException(nameof(configuration), "The JWT Key should be initialized");
    private readonly string _issuer = configuration.GetSection("JWT:Issuer").Value ?? throw new ArgumentNullException(nameof(configuration), "The JWT Issuer should be initialized");
    private readonly string _audience = configuration.GetSection("JWT:Audience").Value ?? throw new ArgumentNullException(nameof(configuration), "The JWT Audience should be initialized");

    /// <summary>
    /// Generates a JWT access token and a refresh token for the specified user.
    /// <para>
    /// The method creates a JWT token containing the user's username, role, and user ID as claims.
    /// The token is signed using the configured secret key and is valid for 24 hours.
    /// A secure refresh token is also generated for session renewal.
    /// </para>
    /// </summary>
    /// <param name="userEntity">The user entity for whom the tokens are generated.</param>
    /// <returns>
    /// A <see cref="TokenResponse"/> containing the access token, its expiration duration, and the refresh token.
    /// </returns>
    /// <remarks>
    /// The JWT token is signed with HMAC SHA256 and includes issuer and audience information from configuration.
    /// </remarks>
    public async Task<TokenResponse> GenerateJwtTokenAsync(User userEntity)
    {
        JwtSecurityTokenHandler tokenHandler = new();
        byte[] key = Encoding.ASCII.GetBytes(_jwtSecret);

        SecurityTokenDescriptor tokenDescriptor = new()
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, userEntity.Username),
                new Claim(ClaimTypes.Role, userEntity.Role),
                new Claim("userId", userEntity.Id.ToString())
            ]),
            Expires = DateTime.UtcNow.AddHours(24),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
        string accessToken = tokenHandler.WriteToken(token);

        string refreshToken = await Task.Run(GenerateRefreshToken);

        return new TokenResponse(accessToken, TimeSpan.FromHours(24), refreshToken);
    }

    /// <summary>
    /// Refreshes the JWT token for the specified user by generating a new access and refresh token.
    /// </summary>
    /// <param name="userEntity">The user entity for whom the tokens are refreshed.</param>
    /// <returns>
    /// A <see cref="TokenResponse"/> containing the new access token, its expiration duration, and the new refresh token.
    /// </returns>
    public async Task<TokenResponse> RefreshJwtTokenAsync(User userEntity) => await GenerateJwtTokenAsync(userEntity);

    /// <summary>
    /// Generates a secure refresh token using a cryptographically strong random number generator.
    /// </summary>
    /// <returns>
    /// A base64-encoded string representing the refresh token.
    /// </returns>
    private static string GenerateRefreshToken()
    {
        byte[] randomBytes = new byte[64];
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}