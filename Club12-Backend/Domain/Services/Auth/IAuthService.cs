using Entities.DTOs.User;
using Entities.Models.UserEntity;

namespace Services.Auth;

/// <summary>
/// Service interface for generating and refreshing JWT tokens.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Generates a JWT token for the given user.
    /// </summary>
    /// <param name="userEntity">The user entity for which to generate the token.</param>
    /// <returns>A <see cref="TokenResponse"/> containing the generated JWT access token and refresh token.</returns>
    Task<TokenResponse> GenerateJwtTokenAsync(User userEntity);

    /// <summary>
    /// Refreshes the JWT token using the provided refresh token.
    /// </summary>
    /// <param name="userEntity">The user entity associated with the refresh token.</param>
    /// <returns>A <see cref="TokenResponse"/> containing the refreshed JWT access token and a new refresh token.</returns>
    Task<TokenResponse> RefreshJwtTokenAsync(User userEntity);
}
