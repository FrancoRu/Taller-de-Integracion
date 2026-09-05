using Domain.Entities.Models;
using Domain.Enums;

using System;

namespace Application.DTOs.MedicalRecord.Response;

/// <summary>
/// The medical-record eligibility state of a player's season registration, flagging not-habilitado players.
/// </summary>
public class MedicalRecordResponse
{
    public required Guid PlayerId { get; set; }
    public required Guid TeamId { get; set; }
    public required Guid TournamentId { get; set; }

    /// <summary>
    /// The medical-record status: Pending, Approved, or Rejected.
    /// </summary>
    public required MedicalRecordStatus Status { get; set; }

    /// <summary>
    /// True only when the record is Approved.
    /// </summary>
    public required bool IsHabilitado { get; set; }

    /// <summary>
    /// Storage reference of the uploaded file, or null if none yet.
    /// </summary>
    public string? FileUrl { get; set; }

    /// <summary>
    /// Original uploaded file name, or null if none yet.
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Reason recorded on rejection, if any.
    /// </summary>
    public string? ReviewReason { get; set; }

    /// <summary>
    /// When the record was last approved or rejected, if ever.
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// Projects a PlayerTeamRegistration into its medical-record view.
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
