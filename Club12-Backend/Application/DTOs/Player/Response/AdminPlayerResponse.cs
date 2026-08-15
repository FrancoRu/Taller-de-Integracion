using System;
namespace Application.DTOs.Player.Response;

/// <summary>
/// Represents a detailed response for an Admin to view player information.
/// Inherits from PublicPlayerResponse and includes sensitive information.
/// </summary>
public class AdminPlayerResponse : PublicPlayerResponse
{
    /// <summary>
    /// Gets or sets the document number of the player.
    /// </summary>
    public required string DocumentNumber { get; set; }

    /// <summary>
    /// Gets or sets the birth date of the player.
    /// </summary>
    public required DateTime BirthDate { get; set; }

    /// <summary>
    /// Gets or sets the phone number of the player.
    /// </summary>
    public required string PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets the social security number or health provider of the player.
    /// </summary>
    public required string SocialSecurity { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the player is federated.
    /// </summary>
    public required bool IsFederated { get; set; }

    /// <summary>
    /// Gets or sets the name of the club the player is affiliated with, if federated.
    /// </summary>
    public required string Club { get; set; }

    /// <summary>
    /// Gets or sets the category the player belongs to, if federated.
    /// </summary>
    public required string Category { get; set; }
}
