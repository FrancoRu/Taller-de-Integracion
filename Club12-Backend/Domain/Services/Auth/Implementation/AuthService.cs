using Entities.DTOs.User;
using Entities.Models.UserEntity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Services.Auth.Implementation;

/// <summary>
/// Service responsible for generating JWT tokens.
/// </summary>
public class AuthService(IConfiguration configuration) : IAuthService
{
    private readonly string _jwtSecret = configuration.GetSection("JWT:Key").Value ?? throw new ArgumentNullException(nameof(configuration), "The JWT Key should be initialized");
    private readonly string _issuer = configuration.GetSection("JWT:Issuer").Value ?? throw new ArgumentNullException(nameof(configuration), "The JWT Issuer should be initialized");
    private readonly string _audience = configuration.GetSection("JWT:Audience").Value ?? throw new ArgumentNullException(nameof(configuration), "The JWT Audience should be initialized");

    public TokenResponse GenerateJwtToken(User userEntity)
    {
        JwtSecurityTokenHandler tokenHandler = new();
        byte[] key = Encoding.ASCII.GetBytes(_jwtSecret);

        SecurityTokenDescriptor tokenDescriptor = new()
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, userEntity.Username),
                new Claim(ClaimTypes.Role, userEntity.Role),
                new Claim("userId", userEntity.Id.ToString())
             }),
            Expires = DateTime.UtcNow.AddSeconds(60),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
        string accessToken = tokenHandler.WriteToken(token);

        TimeSpan expiresIn = TimeSpan.FromSeconds(60);

        // Generate refresh token
        string refreshToken = GenerateRefreshToken();

        return new TokenResponse(accessToken, expiresIn, refreshToken);
    }

    /// <summary>
    /// Generates a random refresh token.
    /// </summary>
    /// <returns>A newly generated refresh token.</returns>
    private static string GenerateRefreshToken()
    {
        byte[] randomBytes = new byte[64];
        using System.Security.Cryptography.RandomNumberGenerator rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
