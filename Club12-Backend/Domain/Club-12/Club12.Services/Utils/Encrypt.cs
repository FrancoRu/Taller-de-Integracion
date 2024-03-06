using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Club12.Services.Utils;

/// <summary>
/// Utility class for encryption operations.
/// </summary>
public static class Encrypt
{
    /// <summary>
    /// Encrypts a given string using the SHA-256 algorithm.
    /// </summary>
    /// <param name="value">The string to be encrypted.</param>
    /// <returns>The SHA-256 hash of the input string.</returns>
    /// <remarks>This method converts the input string to UTF-8 bytes, computes the SHA-256 hash, and returns the hash as a hexadecimal string.</remarks>
    public static string Hash(string value)
    {
        StringBuilder sb = new();
        Encoding enc = Encoding.UTF8;

        byte[] result = SHA256.HashData(enc.GetBytes(value));

        foreach (byte b in result)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Checks if a given plain text password matches a hashed password.
    /// </summary>
    /// <param name="plainTextPassword">The plain text password to check.</param>
    /// <param name="hashedPassword">The hashed password to compare against.</param>
    /// <returns>True if the passwords match, otherwise false.</returns>
    public static bool CheckHash(string plainTextPassword, string hashedPassword)
    {
        string hashedInput = Hash(plainTextPassword);
        return string.Equals(hashedInput, hashedPassword, StringComparison.OrdinalIgnoreCase);
    }

    public static string GenerateJWTToken(string jwtSecret, string userName, string role, Guid id)
    {
        JwtSecurityTokenHandler tokenHandler = new();
        byte[] key = Encoding.ASCII.GetBytes(jwtSecret);

        SecurityTokenDescriptor tokenDescriptor = new()
        {
            Subject = new ClaimsIdentity(new[]
            {
                    new Claim(ClaimTypes.Sid, id.ToString()),
                    new Claim(ClaimTypes.Name, userName),
                    new Claim(ClaimTypes.Role, role)
                }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public static UserClaims? DecodeJWTToken(string jwtToken, string jwtSecret)
    {
        try
        {
            JwtSecurityTokenHandler tokenHandler = new();
            byte[] key = Encoding.ASCII.GetBytes(jwtSecret);

            tokenHandler.ValidateToken(jwtToken, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            JwtSecurityToken jwtTokenClaims = (JwtSecurityToken)validatedToken;

            string? userName = jwtTokenClaims.Claims.FirstOrDefault(claim => claim.Type == "unique_name")?.Value;
            string? role = jwtTokenClaims.Claims.FirstOrDefault(claim => claim.Type == "role")?.Value;
            string? id = jwtTokenClaims.Claims.FirstOrDefault(claim => claim.Type == "Sid")?.Value; 
            
            if (jwtTokenClaims.ValidTo < DateTime.UtcNow)
            {
                return null;
            }

            return userName is null || role is null || id is null ? null : new UserClaims { UserName = userName, Role = role, Id = Guid.Parse(id) };
        }
        catch (Exception)
        {
            return null;
        }
    }

    public class UserClaims
    {
        public required string UserName { get; set; }
        public required string Role { get; set; }
        public required Guid Id { get; set; }
    }
}
