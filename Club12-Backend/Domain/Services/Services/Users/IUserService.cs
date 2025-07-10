using Entities.Models.Users;

namespace Services.Services.Users;

public interface IUserService
{
    /// <summary>
    /// Asynchronously gets the user by its user name.
    /// </summary>
    /// <param name="userName">The username to search for.</param>
    /// <returns>The user entity if found, otherwise null.</returns>
    Task<User?> GetUserByUserNameAsync(string userName);

    /// <summary>
    /// Asynchronously validates user credentials.
    /// </summary>
    /// <param name="userEntity">The user entity containing login credentials.</param>
    /// <param name="plainTextPassword">The plain text password to validate.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is true if the credentials are valid, otherwise false.</returns>
    Task<bool> ValidateCredentialsAsync(User userEntity, string plainTextPassword);

    /// <summary>
    /// Asynchronously gets the user by their refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token to search for.</param>
    /// <returns>The user entity if found, otherwise null.</returns>
    Task<User?> GetUserByRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Updates a user in the database.
    /// </summary>
    /// <param name="user">The user to update.</param>
    /// <returns>A task that represents the asynchronous operation. True if it could be updated, false if not.</returns>
    Task<bool> UpdateUserAsync(User user);
}
