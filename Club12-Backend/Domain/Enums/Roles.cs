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
    public const string Admin = nameof(UserRoleType.ADMIN);
    public const string Owner = nameof(UserRoleType.OWNER);
    public const string TournamentManager = nameof(UserRoleType.TOURNAMENT_MANAGER);
    public const string TeamManager = nameof(UserRoleType.TEAM_MANAGER);
    public const string Guest = nameof(UserRoleType.GUEST);

    /// <summary>
    /// Blog and user management — the two roles that administer
    /// site-wide, non-tournament content and accounts.
    /// </summary>
    public const string AdminOrOwner = $"{Admin},{Owner}";

    /// <summary>
    /// Tournament/division/stage/match/series/venue/sanction/statistic
    /// management — the roles that run a tournament's competitive
    /// structure day to day, plus Admin as the top-privilege role.
    /// </summary>
    public const string AdminOwnerOrTournamentManager = $"{Admin},{Owner},{TournamentManager}";

    /// <summary>
    /// Team and player roster management — a team's own manager, plus the
    /// tournament-level roles that oversee registration.
    /// </summary>
    public const string TeamManagerOrTournamentManagerOrOwner =
        $"{TeamManager},{TournamentManager},{Owner}";

    /// <summary>
    /// Every non-guest staff role — used for read endpoints that expose a
    /// player's private details (document number, birth date, etc.) to
    /// whichever staff role is looking them up.
    /// </summary>
    public const string AnyStaffRole =
        $"{Admin},{Owner},{TournamentManager},{TeamManager}";
}