using Application.DTOs.MedicalRecord.Response;

using System;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Manages the medical-record and eligibility state of a player's season registration.
/// </summary>
public interface IMedicalRecordService
{
    /// <summary>
    /// Records a just-uploaded medical-record file reference on the player's season registration.
    /// </summary>
    Task<MedicalRecordResponse> RecordUploadAsync(
        Guid playerId, Guid teamId, Guid tournamentId, string fileReference, string fileName, string actor);

    /// <summary>
    /// Approves or rejects the medical record.
    /// </summary>
    Task<MedicalRecordResponse> ReviewAsync(
        Guid playerId, Guid teamId, Guid tournamentId, bool approve, string? reason, string actor);

    /// <summary>
    /// Returns the current medical-record and eligibility state for a player's season registration, or null when no such registration exists.
    /// </summary>
    Task<MedicalRecordResponse?> GetAsync(Guid playerId, Guid teamId, Guid tournamentId);
}
