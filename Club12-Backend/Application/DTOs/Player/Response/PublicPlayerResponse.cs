using Application.DTOs.Abstract.Response;

using Domain.Enums;

using System;
namespace Application.DTOs.Player.Response;

/// <summary>
/// Represents a response for a Player, inheriting from the base response.
/// </summary>
public class PublicPlayerResponse : BaseEntityResponse
{
    public required string FirstName { get; set; }

    public required string SecondName { get; set; }

    public required string LastName { get; set; }

    public required string FullName { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public player links.
    /// </summary>
    public required string Slug { get; set; }

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

    /// <summary>
    /// The player's jersey number (dorsal) for this season roster (HU-54).
    /// Null when the roster was not loaded for a specific season or no number
    /// is assigned.
    /// </summary>
    public int? JerseyNumber { get; set; }
}
