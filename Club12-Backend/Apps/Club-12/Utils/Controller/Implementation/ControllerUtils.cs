namespace Club12.Utils.Controller.Implementation;

/// <summary>
/// Utility class for controller operations.
/// </summary>
public class ControllerUtils : IControllerUtils
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ControllerUtils"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The IHttpContextAccessor instance to use for accessing HTTP context.</param>
    public ControllerUtils(
        IHttpContextAccessor httpContextAccessor
    )
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>
    /// Retrieves the user ID from the JWT token in the current HTTP context.
    /// </summary>
    /// <returns>The user ID extracted from the JWT token.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the HTTP context or user is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when unable to retrieve the user ID from the token.</exception>
    public Guid GetUserId()
    {
        if (_httpContextAccessor?.HttpContext?.User == null)
        {
            throw new InvalidOperationException("HTTP context or user is null.");
        }

        string? userIdString = _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

        return string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId)
            ? throw new InvalidOperationException("Unable to retrieve userId from token.")
            : userId;
    }
}
