using Entities.DTOs.User;
using Entities.Models.UserEntity;

namespace Services.Auth;

public interface IAuthService
{
    /// <summary>
    /// Generates a JWT token for a user based on their credentials.
    /// </summary>
    /// <param name="userEntity">The user request containing login credentials.</param>
    /// <returns>The generated JWT token.</returns>
    TokenResponse GenerateJwtToken(User userEntity);
}