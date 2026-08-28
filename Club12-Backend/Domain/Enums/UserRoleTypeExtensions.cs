namespace Domain.Enums;

public static class UserRoleTypeExtensions
{
    public static string ToRoleName(this UserRoleType role)
    {
        return role.ToString();
    }
}
