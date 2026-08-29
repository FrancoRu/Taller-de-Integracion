using Domain.Entities.Models;
using Domain.Enums;

using System;

namespace Application.DTOs.MedicalRecord.Response;

/// <summary>
/// The medical-record / eligibility state of a player's season registration
/// (player + team + tournament). Exposes per-player eligibility so the
/// frontend can warn about not-habilitado players (HU-62).
/// </summary>
public class MedicalRecordResponse
{
    public required Guid PlayerId { get; set; }
    public required Guid TeamId { get; set; }
    public required Guid TournamentId { get; set; }

    /// <summary>The medical-record status (Pending / Approved / Rejected).</summary>
    public required MedicalRecordStatus Status { get; set; }

    /// <summary>True only when the record is Approved (HU-57).</summary>
    public required bool IsHabilitado { get; set; }

    /// <summary>Storage reference of the uploaded file, or null if none yet.</summary>
    public string? FileUrl { get; set; }

    /// <summary>Original uploaded file name, or null if none yet.</summary>
    public string? FileName { get; set; }

    /// <summary>Reason recorded on rejection, if any.</summary>
    public string? ReviewReason { get; set; }

    /// <summary>When the record was last approved/rejected, if ever.</summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// Projects a <see cref="PlayerTeamRegistration"/> into its medical-record
    /// view.
    /// </summary>
    public static MedicalRecordResponse FromRegistration(PlayerTeamRegistration registration)
    {
        return new MedicalRecordResponse
        {
            PlayerId = registration.PlayerId,
            TeamId = registration.TeamId,
            TournamentId = registration.TournamentId,
            Status = registration.MedicalRecordStatus,
            IsHabilitado = registration.IsHabilitado,
            FileUrl = registration.MedicalRecordFileUrl,
            FileName = registration.MedicalRecordFileName,
            ReviewReason = registration.MedicalRecordReviewReason,
            ReviewedAt = registration.MedicalRecordReviewedAt,
        };
    }
}
