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
    /// The player's medical-record status for this season roster; null when not loaded for a specific season.
    /// </summary>
    public MedicalRecordStatus? MedicalRecordStatus { get; set; }

    /// <summary>
    /// Whether the player's medical record is Approved for this season roster, letting the frontend flag it.
    /// </summary>
    public bool IsHabilitado { get; set; }

    /// <summary>
    /// The player's jersey number for this season roster; null when not loaded or not assigned.
    /// </summary>
    public int? JerseyNumber { get; set; }
}
