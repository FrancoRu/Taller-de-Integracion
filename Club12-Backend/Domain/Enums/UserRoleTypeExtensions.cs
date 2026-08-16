using System;

namespace Domain.Enums;

public static class UserRoleTypeExtensions
{
    public static string ToRoleName(this UserRoleType role)
    {
        return role.ToString();
    }

    public static bool IsTeamManagerRole(string? role)
    {
        return string.Equals(role, UserRoleType.TEAM_MANAGER.ToRoleName(), StringComparison.OrdinalIgnoreCase);
    }
}
