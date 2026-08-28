using Application.DTOs.Abstract.Response;

using Domain.Enums;

using System;
namespace Application.DTOs.Player.Response;

/// <summary>
/// Represents a response for a Player, inheriting from the base response.
/// </summary>
public class PublicPlayerResponse : BaseEntityResponse
{
    /// <summary>
    /// The first name of the Player.
    /// </summary>
    public required string FirstName { get; set; }

    /// <summary>
    /// The second name of the Player.
    /// </summary>
    public required string SecondName { get; set; }

    /// <summary>
    /// The last name of the player.
    /// </summary>
    public required string LastName { get; set; }

    /// <summary>
    /// The full name of the player.
    /// </summary>
    public required string FullName { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public player links.
    /// </summary>
    public required string Slug { get; set; }

    /// <summary>
    /// The unique identifier of the Team to which the Player belongs.
    /// </summary>
    public required Guid TeamId { get; set; }

    /// <summary>
    /// The player's medical-record status for the season roster this response
    /// belongs to (HU-57). Null when the roster was not loaded for a specific
    /// season.
    /// </summary>
    public MedicalRecordStatus? MedicalRecordStatus { get; set; }

    /// <summary>
    /// Whether the player is habilitado (medical record Approved) for this
    /// season roster (HU-57), so the frontend can flag not-habilitado players
    /// (HU-62).
    /// </summary>
    public bool IsHabilitado { get; set; }
}
