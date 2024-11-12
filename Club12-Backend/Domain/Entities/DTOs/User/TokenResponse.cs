namespace Entities.DTOs.User;

/// <summary>
/// Represents the response containing access token, expiration time, and refresh token.
/// </summary>
public sealed class TokenResponse(string AccessToken, TimeSpan ExpiresIn, string RefreshToken)
{
    /// <summary>
    /// The access token used for authorization.
    /// </summary>
    public string AccessToken { get; init; } = AccessToken;

    /// <summary>
    /// The time span until the access token expires.
    /// </summary>
    public TimeSpan ExpiresIn { get; init; } = ExpiresIn;

    /// <summary>
    /// The refresh token used to obtain a new access token.
    /// </summary>
    public string RefreshToken { get; init; } = RefreshToken;
}
