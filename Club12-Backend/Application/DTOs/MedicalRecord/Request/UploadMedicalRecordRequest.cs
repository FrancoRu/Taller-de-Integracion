using Microsoft.AspNetCore.Http;

using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.MedicalRecord.Request;

/// <summary>
/// Multipart request to upload a player's medical-record file (PDF) for a
/// specific team and tournament (HU-55). The record is attached to the
/// player's season registration (player + team + tournament), never to the
/// player globally, so the same player in another team/tournament keeps a
/// separate record.
/// </summary>
public class UploadMedicalRecordRequest
{
    /// <summary>The player the medical record belongs to.</summary>
    [Required]
    public required Guid PlayerId { get; set; }

    /// <summary>The team the player is registered to for the season.</summary>
    [Required]
    public required Guid TeamId { get; set; }

    /// <summary>The tournament (season) the record applies to.</summary>
    [Required]
    public required Guid TournamentId { get; set; }

    /// <summary>The medical-record file (PDF) to upload.</summary>
    [Required]
    public required IFormFile File { get; set; }
}
