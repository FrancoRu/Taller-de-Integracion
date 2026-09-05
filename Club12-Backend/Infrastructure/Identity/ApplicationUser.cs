using Microsoft.AspNetCore.Identity;

using System;

namespace Infrastructure.Identity;

/// <summary>
/// Identity user for authentication, kept separate from domain entities.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// Opaque refresh token stored server-side.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// UTC expiry for the stored refresh token.
    /// </summary>
    public DateTime? RefreshTokenExpiryTime { get; set; }

    /// <summary>
    /// The Id of the OWNER who registered this account, when it was created by an owner rather than an admin.
    /// </summary>
    public Guid? CreatedByOwnerId { get; set; }

    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    public bool MustChangePassword { get; set; }
}
