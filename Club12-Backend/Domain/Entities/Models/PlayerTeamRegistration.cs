using Domain.Enums;

using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Models;

/// <summary>
/// Links a Player to a Team for exactly one season, the source of truth for roster membership, unlike the denormalized Player.TeamId pointer.
/// </summary>
public class PlayerTeamRegistration : EntityBase
{
    public required Guid PlayerId { get; set; }
    public Player? Player { get; set; }

    public required Guid TeamId { get; set; }
    public Team? Team { get; set; }

    /// <summary>
    /// The season this registration belongs to, captured at registration time from the Team's TournamentId at that moment.
    /// </summary>
    public required Guid TournamentId { get; set; }
    public Tournament? Tournament { get; set; }

    /// <summary>
    /// The player's jersey number for this season registration, null when no number has been assigned yet.
    /// </summary>
    public int? JerseyNumber { get; set; }

    /// <summary>
    /// Medical-record eligibility status for this season registration, defaulting to Pending.
    /// </summary>
    public MedicalRecordStatus MedicalRecordStatus { get; set; } = MedicalRecordStatus.Pending;

    /// <summary>
    /// Storage reference of the uploaded medical-record file for this season, null until a file is uploaded.
    /// </summary>
    public string? MedicalRecordFileUrl { get; set; }

    /// <summary>
    /// Original file name of the uploaded medical-record file, kept for display.
    /// </summary>
    public string? MedicalRecordFileName { get; set; }

    /// <summary>
    /// Optional reason recorded when the medical record is rejected.
    /// </summary>
    public string? MedicalRecordReviewReason { get; set; }

    /// <summary>
    /// When the medical record was last approved or rejected, null while still Pending.
    /// </summary>
    public DateTime? MedicalRecordReviewedAt { get; set; }

    /// <summary>
    /// Refs written before the private-bucket relocation lived under this prefix inside public-images and no longer resolve.
    /// </summary>
    public const string LegacyReferencePrefix = "medical-records/";

    /// <summary>
    /// Whether fileReference is a real, resolvable stored file reference: non-null, non-whitespace, and not a legacy prefix ref.
    /// </summary>
    public static bool IsStoredReference(string? fileReference) =>
        !string.IsNullOrWhiteSpace(fileReference)
        && !fileReference.StartsWith(LegacyReferencePrefix, StringComparison.Ordinal);

    /// <summary>
    /// Whether this registration's player is habilitado for this season: Approved status and a real stored file reference.
    /// </summary>
    [NotMapped]
    public bool IsHabilitado =>
        MedicalRecordStatus == Enums.MedicalRecordStatus.Approved && IsStoredReference(MedicalRecordFileUrl);
}
