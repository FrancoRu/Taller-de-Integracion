using System;

namespace Domain.Enums;

public static class UserRoleTypeExtensions
{
    public static string ToRoleName(this UserRoleType role) => role.ToString();

    public static bool IsTeamManagerRole(string? role)
        => string.Equals(role, UserRoleType.TEAM_MANAGER.ToRoleName(), StringComparison.OrdinalIgnoreCase);
}
