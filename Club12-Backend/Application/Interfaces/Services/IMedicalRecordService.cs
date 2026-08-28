using Application.DTOs.MedicalRecord.Response;

using System;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Manages the medical-record / eligibility state of a player's season
/// registration (player + team + tournament) — HU-55/57/58/59/62. The record
/// lives on the <c>PlayerTeamRegistration</c>, so it is inherently per season
/// and never carried over between tournaments.
/// </summary>
public interface IMedicalRecordService
{
    /// <summary>
    /// Records a just-uploaded medical-record file reference on the player's
    /// season registration (HU-55). Does NOT habilitate the player — the
    /// status stays/returns to Pending until an owner/admin approves it
    /// (HU-57). Throws when no registration exists for that player + team +
    /// tournament.
    /// </summary>
    Task<MedicalRecordResponse> RecordUploadAsync(
        Guid playerId, Guid teamId, Guid tournamentId, string fileReference, string fileName, string actor);

    /// <summary>
    /// Approves or rejects the medical record (HU-58). Approve → status
    /// Approved (player habilitado, HU-57); reject → status Rejected with the
    /// optional reason. Throws when no matching registration exists.
    /// </summary>
    Task<MedicalRecordResponse> ReviewAsync(
        Guid playerId, Guid teamId, Guid tournamentId, bool approve, string? reason, string actor);

    /// <summary>
    /// Returns the current medical-record / eligibility state for a player's
    /// season registration (HU-62), or null when no such registration exists.
    /// </summary>
    Task<MedicalRecordResponse?> GetAsync(Guid playerId, Guid teamId, Guid tournamentId);
}
