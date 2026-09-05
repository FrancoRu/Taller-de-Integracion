using System;
using System.Security.Claims;

namespace API.Utils.Helpers;

public static class AuthHelper
{
    /// <summary>
    /// Extracts the caller's role and id from the Role and NameIdentifier claims
    /// of an authenticated <see cref="ClaimsPrincipal"/>. Assumes both claims are
    /// present: an unauthenticated or malformed principal makes
    /// <see cref="Guid.Parse(string)"/> throw <see cref="FormatException"/> rather
    /// than returning a default value.
    /// </summary>
    public static (string role, Guid id) GetCallerClaims(this ClaimsPrincipal principal)
    {
        string role = principal.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        string sub = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        return (role, Guid.Parse(sub));
    }
}
