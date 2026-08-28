namespace Domain.Enums;

/// <summary>
/// Compile-time string constants for every UserRoleType value.
/// Use these in [Authorize(Roles = ...)] to avoid magic strings.
/// <para>
/// Constants use nameof() so a rename of the enum member produces
/// a compile error here instead of a silent runtime mismatch.
/// </para>
/// <para>
/// HU-05: the role model is deliberately just two operator accounts —
/// <see cref="Owner"/> and <see cref="Admin"/> (Admin IT) — plus the
/// technical <see cref="Guest"/> role for anonymous public access. The
/// former TournamentManager / TeamManager roles were removed, so every
/// staff-only endpoint now resolves to <see cref="AdminOrOwner"/>.
/// </para>
/// </summary>
public static class Roles
{
    public const string Admin = nameof(UserRoleType.ADMIN);
    public const string Owner = nameof(UserRoleType.OWNER);
    public const string Guest = nameof(UserRoleType.GUEST);

    /// <summary>
    /// Every staff role that may operate the panel: the Owner account and
    /// the Admin IT account. This is the single group every staff-only
    /// endpoint uses now that the tournament/team manager roles are gone —
    /// blog and user management, tournament/division/stage/match/series/
    /// venue/sanction/statistic management, team and roster management, and
    /// the endpoints that expose a player's private details all collapse to
    /// this same Owner-or-Admin group.
    /// </summary>
    public const string AdminOrOwner = $"{Admin},{Owner}";
}
