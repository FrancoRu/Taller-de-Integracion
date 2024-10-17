using Club12.Entities.UserEntity;

namespace Club12.Services.Services.UserService;

public interface IUserService
{
    /// <summary>
    /// Gets the user by its user name.
    /// </summary>
    /// <param name="userName"></param>
    /// <returns>The user entity if found, otherwise null.</returns>
    User? GetUserByUserNameAsync(string userName);

    /// <summary>
    /// Validates user credentials.
    /// </summary>
    /// <param name="userEntity">The user request containing login credentials.</param>
    /// <param name="plainTextPassword">The user request password.</param>
    /// <returns>True if the credentials are valid, otherwise false.</returns>
    bool ValidateCredentials(User userEntity, string plainTextPassword);
}
