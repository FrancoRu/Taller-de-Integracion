namespace Application.Utils.Constants.Auth;

/// <summary>
/// Application-specific JWT claim types that are not part of the standard
/// System.Security.Claims.ClaimTypes set. Shared between the
/// token-issuing code (Infrastructure.Identity) and the code that reads the
/// claim back (API middleware), so a typo can't silently desync the two.
/// </summary>
public static class CustomClaimTypes
{
    /// <summary>
    /// Boolean-valued ("true"/"false") claim indicating the user must change
    /// their password before accessing most endpoints.
    /// </summary>
    public const string MustChangePassword = "must_change_password";
}
