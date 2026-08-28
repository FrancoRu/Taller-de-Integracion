using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.MedicalRecord.Request;

/// <summary>
/// Owner/admin request to approve or reject a player's medical record for a
/// team and tournament (HU-58). Approving makes the player habilitado for that
/// team+tournament (HU-57); rejecting leaves them not-habilitado, optionally
/// with a reason.
/// </summary>
public class ReviewMedicalRecordRequest
{
    /// <summary>The player whose medical record is being reviewed.</summary>
    [Required]
    public required Guid PlayerId { get; set; }

    /// <summary>The team the player is registered to for the season.</summary>
    [Required]
    public required Guid TeamId { get; set; }

    /// <summary>The tournament (season) the record applies to.</summary>
    [Required]
    public required Guid TournamentId { get; set; }

    /// <summary>
    /// True to approve (player becomes habilitado); false to reject.
    /// </summary>
    [Required]
    public required bool Approve { get; set; }

    /// <summary>
    /// Optional reason, typically recorded when rejecting.
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }
}
