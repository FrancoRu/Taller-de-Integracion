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
    /// Generates a JWT token for a user based on their credentials.
    /// </summary>
    /// <param name="userEntity">The user request containing login credentials.</param>
    /// <returns>The generated JWT token.</returns>
    string GenerateJwtToken(User userEntity);

    /// <summary>
    /// Checks if the user has SuperAdmin role.
    /// </summary>
    /// <param name="jwtToken"></param>
    /// <returns></returns>
    bool IsSuperAdmin(string jwtToken);

    /// <summary>
    /// Checks if the user has SuperAdmin or Admin role.
    /// </summary>
    /// <param name="jwtToken"></param>
    /// <returns></returns>
    bool IsAuthenticated(string jwtToken);

    /// <summary>
    /// Validates user credentials.
    /// </summary>
    /// <param name="userEntity">The user request containing login credentials.</param>
    /// <param name="plainTextPassword">The user request password.</param>
    /// <returns>True if the credentials are valid, otherwise false.</returns>
    bool ValidateCredentials(User userEntity, string plainTextPassword);

    /// <summary>
    /// Validates user token.
    /// </summary>
    /// <param name="jwtToken">The token of the user who makes a request.</param>
    /// <returns>True if the token contains the id of a user that is registered in the database.</returns>
    bool ValidateToken(string jwtToken);
}
