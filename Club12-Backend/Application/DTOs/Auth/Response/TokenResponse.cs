using System;

namespace Application.DTOs.Auth.Response;

/// <summary>
/// Represents the response containing access token, expiration time, and refresh token.
/// </summary>
public sealed class TokenResponse(string accessToken, TimeSpan expiresIn, string? refreshToken)
{
    /// <summary>
    /// The access token used for authorization.
    /// </summary>
    public string AccessToken { get; init; } = accessToken;

    /// <summary>
    /// The time span until the access token expires.
    /// </summary>
    public TimeSpan ExpiresIn { get; init; } = expiresIn;

    /// <summary>
    /// The refresh token used to obtain a new access token.
    /// </summary>
    public string? RefreshToken { get; init; } = refreshToken;
}
