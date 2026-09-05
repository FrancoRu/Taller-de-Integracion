using System;
namespace Application.DTOs.Player.Response;

/// <summary>
/// Represents a detailed response for an Admin to view player information.
/// Inherits from PublicPlayerResponse and includes sensitive information.
/// </summary>
public class AdminPlayerResponse : PublicPlayerResponse
{
    public required string DocumentNumber { get; set; }

    public required DateTime BirthDate { get; set; }

    public required string PhoneNumber { get; set; }

    /// <summary>The player's health insurance provider or social work plan.</summary>
    public required string SocialSecurity { get; set; }

    public required bool IsFederated { get; set; }

    /// <summary>The club the player is affiliated with; only meaningful when <see cref="IsFederated"/> is true.</summary>
    public required string Club { get; set; }

    /// <summary>The player's federation category; only meaningful when <see cref="IsFederated"/> is true.</summary>
    public required string Category { get; set; }
}
