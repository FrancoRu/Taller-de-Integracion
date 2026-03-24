using Microsoft.AspNetCore.Identity;
using System;

namespace Infrastructure.Identity;

/// <summary>
/// Identity user for authentication (separate from domain entities).
/// Roles are managed exclusively via <see cref="IdentityRole{TKey}"/>.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>Opaque refresh token stored server-side.</summary>
    public string? RefreshToken { get; set; }

    /// <summary>UTC expiry for the stored refresh token.</summary>
    public DateTime? RefreshTokenExpiryTime { get; set; }

    /// <summary>
    /// For TOURNAMENT_MANAGER accounts: the Id of the OWNER who registered them.
    /// Null for ADMIN and OWNER accounts (created directly by an admin).
    /// </summary>
    public Guid? CreatedByOwnerId { get; set; }

    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    public bool MustChangePassword { get; set; }
}
