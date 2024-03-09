namespace Club12.Utils.Controller;

/// <summary>
/// Interface for utility class providing controller operations.
/// </summary>
public interface IControllerUtils
{
    /// <summary>
    /// Retrieves the user ID from the JWT token in the current HTTP context.
    /// </summary>
    /// <returns>The user ID extracted from the JWT token.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the HTTP context or user is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when unable to retrieve the user ID from the token.</exception>
    Guid GetUserId();
}
