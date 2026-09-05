namespace Application.Utils.Constants.Auth;

/// <summary>
/// Application-specific JWT claim types that are not part of the standard ClaimTypes set.
/// </summary>
public static class CustomClaimTypes
{
    /// <summary>
    /// Boolean claim, stored as the string true or false, indicating the user must change their password before most endpoints.
    /// </summary>
    public const string MustChangePassword = "must_change_password";
}
