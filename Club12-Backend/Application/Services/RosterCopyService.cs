using Application.DTOs.Roster.Response;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;

using Domain.Constants;
using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// Copies a roster from a previous season into a new season (HU-53). Only
/// season-scoped <see cref="PlayerTeamRegistration"/> rows are created; the
/// Player rows themselves (the person) are reused untouched, medical records
/// start fresh (HU-59), and sanctions are never carried over.
/// </summary>
public class RosterCopyService(IUnitOfWork unitOfWork) : IRosterCopyService
{
    private readonly IPlayerTeamRegistrationRepository _registrationRepository = unitOfWork.PlayerTeamRegistrationRepository;

    /// <inheritdoc />
    public async Task<RosterCopyResult> CopyRosterAsync(
        Guid sourceTeamId, Guid sourceTournamentId, Guid targetTeamId, Guid targetTournamentId)
    {
        List<PlayerTeamRegistration> sourceRegistrations = [.. await _registrationRepository.FindAsync(
            registration => registration.TeamId == sourceTeamId
                && registration.TournamentId == sourceTournamentId)];

        if (sourceRegistrations.Count == 0)
        {
            return new RosterCopyResult { CopiedCount = 0, SkippedCount = 0 };
        }

        // A player may hold at most one registration per tournament (unique
        // PlayerId+TournamentId index). Any source player already registered to
        // the target season is skipped, which is what makes the copy idempotent
        // and prevents an HU-54 two-teams-in-one-season violation.
        HashSet<Guid> alreadyInTargetSeason = [.. (await _registrationRepository.FindAsync(
                registration => registration.TournamentId == targetTournamentId))
            .Select(registration => registration.PlayerId)];

        List<PlayerTeamRegistration> newRegistrations = [.. sourceRegistrations
            .Where(source => !alreadyInTargetSeason.Contains(source.PlayerId))
            .Select(source => new PlayerTeamRegistration
            {
                Id = Guid.Empty,
                PlayerId = source.PlayerId,
                TeamId = targetTeamId,
                TournamentId = targetTournamentId,
                // JerseyNumber, MedicalRecordStatus (defaults Pending) and all
                // medical fields are intentionally NOT copied — each season
                // starts with a fresh, un-habilitado registration (HU-59).
                DateCreated = DateTime.UtcNow,
                CreatedBy = AuditConstants.SystemUser,
            })];

        if (newRegistrations.Count > 0)
        {
            await _registrationRepository.AddRangeAsync(newRegistrations);
        }

        return new RosterCopyResult
        {
            CopiedCount = newRegistrations.Count,
            SkippedCount = sourceRegistrations.Count - newRegistrations.Count,
        };
    }
}
