using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.MedicalRecord.Request;

/// <summary>
/// Owner or admin request to approve or reject a player's medical record for a team and tournament.
/// </summary>
public class ReviewMedicalRecordRequest
{
    /// <summary>
    /// The player whose medical record is being reviewed.
    /// </summary>
    [Required]
    public required Guid PlayerId { get; set; }

    /// <summary>
    /// The team the player is registered to for the season.
    /// </summary>
    [Required]
    public required Guid TeamId { get; set; }

    /// <summary>
    /// The tournament the record applies to.
    /// </summary>
    [Required]
    public required Guid TournamentId { get; set; }

    /// <summary>
    /// True to approve and make the player habilitado; false to reject.
    /// </summary>
    [Required]
    public required bool Approve { get; set; }

    /// <summary>
    /// Optional reason, typically recorded when rejecting.
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }
}
