namespace Domain.Enums;

/// <summary>
/// Compile-time string constants for every UserRoleType value.
/// Use these in [Authorize(Roles = ...)] to avoid magic strings.
/// <para>
/// Constants use nameof() so a rename of the enum member produces
/// a compile error here instead of a silent runtime mismatch.
/// </para>
/// </summary>
public static class Roles
{
    public const string Admin             = nameof(UserRoleType.ADMIN);
    public const string Owner             = nameof(UserRoleType.OWNER);
    public const string TournamentManager = nameof(UserRoleType.TOURNAMENT_MANAGER);
    public const string TeamManager       = nameof(UserRoleType.TEAM_MANAGER);
    public const string Guest             = nameof(UserRoleType.GUEST);

    // ── Composite constants for multi-role [Authorize] attributes ──
    public const string AdminOrOwner = $"{Admin},{Owner}";
}