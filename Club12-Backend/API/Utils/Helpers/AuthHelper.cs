using System;
using System.Security.Claims;

namespace API.Utils.Helpers;

public static class AuthHelper
{
    public static (string role, Guid id) GetCallerClaims(this ClaimsPrincipal principal)
    {
        string role = principal.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        string sub  = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        return (role, Guid.Parse(sub));
    }
}
