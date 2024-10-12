namespace Club12.Entities.TokenResponse;

/// <summary>
/// Represents the response containing access token, expiration time, and refresh token.
/// </summary>
public sealed record TokenResponse(string AccessToken, TimeSpan ExpiresIn);