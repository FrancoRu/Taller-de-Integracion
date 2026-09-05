namespace Domain.Enums;

/// <summary>
/// Compile-time string constants for every UserRoleType value, used in authorization attributes to avoid magic strings.
/// </summary>
public static class Roles
{
    public const string Admin = nameof(UserRoleType.ADMIN);
    public const string Owner = nameof(UserRoleType.OWNER);
    public const string Guest = nameof(UserRoleType.GUEST);

    /// <summary>
    /// Every staff role that may operate the panel: the Owner account and the Admin IT account.
    /// </summary>
    public const string AdminOrOwner = $"{Admin},{Owner}";
}
