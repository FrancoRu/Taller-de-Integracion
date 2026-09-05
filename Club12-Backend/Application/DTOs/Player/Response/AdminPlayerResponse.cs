using System;
namespace Application.DTOs.Player.Response;

/// <summary>
/// Detailed response for an admin to view player information, extending PublicPlayerResponse with sensitive fields.
/// </summary>
public class AdminPlayerResponse : PublicPlayerResponse
{
    public required string DocumentNumber { get; set; }

    public required DateTime BirthDate { get; set; }

    public required string PhoneNumber { get; set; }

    /// <summary>
    /// The player's health insurance provider or social work plan.
    /// </summary>
    public required string SocialSecurity { get; set; }

    public required bool IsFederated { get; set; }

    /// <summary>
    /// The club the player is affiliated with; only meaningful when IsFederated is true.
    /// </summary>
    public required string Club { get; set; }

    /// <summary>
    /// The player's federation category; only meaningful when IsFederated is true.
    /// </summary>
    public required string Category { get; set; }
}
