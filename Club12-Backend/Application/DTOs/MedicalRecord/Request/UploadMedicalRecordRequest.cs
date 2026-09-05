using Microsoft.AspNetCore.Http;

using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.MedicalRecord.Request;

/// <summary>
/// Multipart request to upload a player's medical-record PDF for a specific team and tournament.
/// </summary>
public class UploadMedicalRecordRequest
{
    /// <summary>
    /// The player the medical record belongs to.
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
    /// The medical-record PDF to upload.
    /// </summary>
    [Required]
    public required IFormFile File { get; set; }
}
