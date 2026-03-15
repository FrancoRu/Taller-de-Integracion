using Microsoft.AspNetCore.Identity;
using System;

namespace Infrastructure.Identity;

/// <summary>
/// Identity user for authentication (separate from domain entities).
/// Roles are managed exclusively via <see cref="IdentityRole{TKey}"/> —
/// use <c>UserManager.GetRolesAsync</c> instead of a flat Role property.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>Opaque refresh token stored server-side.</summary>
    public string? RefreshToken { get; set; }

    /// <summary>UTC expiry for the stored refresh token.</summary>
    public DateTime? RefreshTokenExpiryTime { get; set; }
}
