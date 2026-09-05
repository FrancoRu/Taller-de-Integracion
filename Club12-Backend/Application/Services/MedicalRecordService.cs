using Application.DTOs.MedicalRecord.Response;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Constants;

using Domain.Entities.Models;
using Domain.Enums;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// Applies medical-record and eligibility operations to a player's season registration.
/// </summary>
public class MedicalRecordService(IUnitOfWork unitOfWork) : IMedicalRecordService
{
    private readonly IPlayerTeamRegistrationRepository _registrationRepository = unitOfWork.PlayerTeamRegistrationRepository;

    /// <summary>
    /// Records a new ficha médica upload and resets it to MedicalRecordStatus.Pending for review.
    /// </summary>
    public async Task<MedicalRecordResponse> RecordUploadAsync(
        Guid playerId, Guid teamId, Guid tournamentId, string fileReference, string fileName, string actor)
    {
        PlayerTeamRegistration registration = await GetRegistrationAsync(playerId, teamId, tournamentId);

        // Once the ficha is Approved against a real stored file, the player is habilitado and the record is frozen so it can only be viewed or downloaded, never replaced by a new upload; an Approved row with a legacy or unresolvable reference from before the private-bucket relocation never actually habilitated the player and would otherwise have no path to fix it, so re-upload stays allowed for that shape.
        if (registration.MedicalRecordStatus == MedicalRecordStatus.Approved
            && PlayerTeamRegistration.IsStoredReference(registration.MedicalRecordFileUrl))
        {
            throw new InvalidOperationException(ErrorMessages.MedicalRecord.AlreadyApproved);
        }

        registration.MedicalRecordFileUrl = fileReference;
        registration.MedicalRecordFileName = fileName;
        // Uploading a new file always requires a fresh review — it never habilitates on its own.
        registration.MedicalRecordStatus = MedicalRecordStatus.Pending;
        registration.MedicalRecordReviewReason = null;
        registration.MedicalRecordReviewedAt = null;
        Touch(registration, actor);

        await _registrationRepository.UpdateAsync(registration);

        return MedicalRecordResponse.FromRegistration(registration);
    }

    /// <summary>
    /// Approves or rejects a ficha médica, blocking approval unless the ficha points at a stored file.
    /// </summary>
    public async Task<MedicalRecordResponse> ReviewAsync(
        Guid playerId, Guid teamId, Guid tournamentId, bool approve, string? reason, string actor)
    {
        PlayerTeamRegistration registration = await GetRegistrationAsync(playerId, teamId, tournamentId);

        // A ficha can only be approved against a file that is actually stored; refs under the legacy medical-records prefix point into the old public bucket and no longer resolve, so they do not count as stored, though rejecting with no file stays legal since only the approve transition is guarded.
        if (approve && !PlayerTeamRegistration.IsStoredReference(registration.MedicalRecordFileUrl))
        {
            throw new InvalidOperationException(ErrorMessages.MedicalRecord.NoStoredFile);
        }

        registration.MedicalRecordStatus = approve ? MedicalRecordStatus.Approved : MedicalRecordStatus.Rejected;
        registration.MedicalRecordReviewReason = approve ? null : reason;
        registration.MedicalRecordReviewedAt = DateTime.UtcNow;
        Touch(registration, actor);

        await _registrationRepository.UpdateAsync(registration);

        return MedicalRecordResponse.FromRegistration(registration);
    }

    public async Task<MedicalRecordResponse?> GetAsync(Guid playerId, Guid teamId, Guid tournamentId)
    {
        PlayerTeamRegistration? registration = await FindRegistrationAsync(playerId, teamId, tournamentId);
        return registration is null ? null : MedicalRecordResponse.FromRegistration(registration);
    }

    private async Task<PlayerTeamRegistration> GetRegistrationAsync(Guid playerId, Guid teamId, Guid tournamentId)
    {
        return await FindRegistrationAsync(playerId, teamId, tournamentId)
            ?? throw new InvalidOperationException(
                ErrorMessages.MedicalRecord.RegistrationNotFound(playerId, teamId, tournamentId));
    }

    private async Task<PlayerTeamRegistration?> FindRegistrationAsync(Guid playerId, Guid teamId, Guid tournamentId)
    {
        return (await _registrationRepository.FindAsync(
            registration => registration.PlayerId == playerId
                && registration.TeamId == teamId
                && registration.TournamentId == tournamentId))
            .FirstOrDefault();
    }

    private static void Touch(PlayerTeamRegistration registration, string actor)
    {
        registration.DateUpdated = DateTime.UtcNow;
        registration.UpdatedBy = string.IsNullOrWhiteSpace(actor) ? registration.UpdatedBy : actor;
    }
}
