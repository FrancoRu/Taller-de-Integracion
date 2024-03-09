using Club12.Entities.UserEntity;

namespace Club12.Services.Users;

public interface IUserService
{
    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="userEntity">The user request containing user information.</param>
    /// <returns>The created user response.</returns>
    User CreateUser(User userEntity);

    /// <summary>
    /// Retrieves a user by their username.
    /// </summary>
    /// <param name="userId">The id of the user to retrieve.</param>
    /// <returns>The user response with the specified username, or null if not found.</returns>
    User? GetUserById(Guid userId);

    /// <summary>
    /// Gets the user by its user name.
    /// </summary>
    /// <param name="userName"></param>
    /// <returns>The user entity if found, otherwhise null.</returns>
    User? GetUserByUserName(string userName);

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <param name="userEntity">The user request containing updated user information.</param>
    /// <returns>The updated user response.</returns>
    Task<bool> UpdateUser(User userEntity);

    /// <summary>
    /// Deletes a user by their username.
    /// </summary>
    /// <param name="userEntity">The username of the user to delete.</param>
    void DeleteUser(User userEntity);

    /// <summary>
    /// Validates user credentials.
    /// </summary>
    /// <param name="userEntity">The user request containing login credentials.</param>
    /// <param name="plainTextPassword">The user request password.</param>
    /// <returns>True if the credentials are valid, otherwise false.</returns>
    bool ValidateCredentials(User userEntity, string plainTextPassword);
}
