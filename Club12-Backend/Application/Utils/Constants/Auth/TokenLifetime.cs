namespace Application.Utils.Constants.Auth;

/// <summary>
/// Durations and sizes applied when issuing JWT access/refresh tokens.
/// </summary>
public static class TokenLifetime
{
    public const int AccessTokenExpiryHours = 24;

    /// <summary>
    /// Size, in bytes, of the cryptographically random refresh token.
    /// </summary>
    public const int RefreshTokenByteLength = 64;
}
