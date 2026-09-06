using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Constants;

using Domain.Constants;
using Domain.Entities.Models;
using Domain.Enums;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// Manages a division's team roster, independent of any stage placement.
/// </summary>
public class DivisionRosterService(IUnitOfWork unitOfWork, IStageService stageService) : IDivisionRosterService
{
    private readonly IDivisionTeamRegistrationRepository _registrationRepository = unitOfWork.DivisionTeamRegistrationRepository;
    private readonly IStageTeamMatchRepository _stageTeamMatchRepository = unitOfWork.StageTeamMatchRepository;
    private readonly IDivisionRepository _divisionRepository = unitOfWork.DivisionRepository;

    /// <inheritdoc/>
    public async Task<List<Team>> GetRosterAsync(Guid divisionId)
    {
        IEnumerable<DivisionTeamRegistration> registrations = await _registrationRepository.FindAsync(
            r => r.DivisionId == divisionId,
            includes: [r => r.Team!]);

        return [.. registrations.Select(r => r.Team!)];
    }

    /// <inheritdoc/>
    public async Task<List<DivisionTeamRegistration>> EnrollTeamsAsync(Guid divisionId, List<Guid> teamIds)
    {
        await EnsureDivisionStructureEditableAsync(divisionId);

        Division targetDivision = await _divisionRepository.GetByIdAsync(divisionId)
            ?? throw new InvalidOperationException(ErrorMessages.Stage.DivisionNotFound);

        List<Guid> distinctIds = [.. teamIds.Distinct()];

        IEnumerable<DivisionTeamRegistration> existingForDivision = await _registrationRepository.FindAsync(
            r => r.DivisionId == divisionId && distinctIds.Contains(r.TeamId));

        HashSet<Guid> alreadyRegistered = [.. existingForDivision.Select(r => r.TeamId)];

        List<Guid> newIds = [.. distinctIds.Where(id => !alreadyRegistered.Contains(id))];

        if (newIds.Count == 0)
        {
            return [];
        }

        await EnsureNoConflictingRegistrationAsync(targetDivision, newIds);

        List<DivisionTeamRegistration> newRegistrations = [.. newIds.Select(teamId => new DivisionTeamRegistration
        {
            TeamId = teamId,
            DivisionId = divisionId,
            CreatedBy = AuditConstants.SystemUser,
        })];

        await _registrationRepository.AddRangeAsync(newRegistrations);

        return newRegistrations;
    }

    /// <inheritdoc/>
    public async Task UnenrollTeamsAsync(Guid divisionId, List<Guid> teamIds)
    {
        if (teamIds == null || teamIds.Count == 0)
        {
            return;
        }

        await EnsureDivisionStructureEditableAsync(divisionId);

        await _stageTeamMatchRepository.RemoveAsync(stm =>
            teamIds.Contains(stm.TeamId) && stm.Stage!.DivisionId == divisionId);

        await _registrationRepository.RemoveAsync(r =>
            r.DivisionId == divisionId && teamIds.Contains(r.TeamId));
    }

    /// <inheritdoc/>
    public Task<List<Stage>> RebuildSubGroupsAsync(Guid divisionId, int subGroupCount) =>
        stageService.RebuildSubGroupsAsync(divisionId, subGroupCount);

    /// <inheritdoc/>
    public Task AutoDistributeRosterAsync(Guid divisionId) =>
        stageService.AutoDistributeRosterAsync(divisionId);

    /// <inheritdoc/>
    public Task ReassignTeamToSubGroupAsync(Guid teamId, Guid fromStageId, Guid toStageId) =>
        stageService.ReassignTeamToSubGroupAsync(teamId, fromStageId, toStageId);

    /// <summary>
    /// Guards roster edits against the state of the division's tournament, mirroring StageService's structure lock.
    /// </summary>
    private async Task EnsureDivisionStructureEditableAsync(Guid divisionId)
    {
        Division? division = await _divisionRepository.GetByIdAsync(
            divisionId, includes: [d => d.Tournament]);

        if (division?.Tournament is null)
        {
            return;
        }

        bool structureLocked = division.Tournament.Status
            is TournamentStatus.Ongoing or TournamentStatus.Finished or TournamentStatus.Canceled;

        if (structureLocked)
        {
            throw new InvalidOperationException(ErrorMessages.Stage.StructureLockedTournamentStarted);
        }
    }

    /// <summary>
    /// Throws when a team already holds a same-kind registration, regular or cross-cup, in a different division of the same tournament.
    /// </summary>
    private async Task EnsureNoConflictingRegistrationAsync(Division targetDivision, List<Guid> teamIds)
    {
        IEnumerable<DivisionTeamRegistration> candidates = await _registrationRepository.FindAsync(
            r => teamIds.Contains(r.TeamId) && r.DivisionId != targetDivision.Id,
            includes: [r => r.Division!]);

        List<Guid> conflictingTeamIds = [.. candidates
            .Where(r => r.Division!.TournamentId == targetDivision.TournamentId
                && r.Division!.IsCrossDivisionCup == targetDivision.IsCrossDivisionCup)
            .Select(r => r.TeamId)
            .Distinct()];

        if (conflictingTeamIds.Count > 0)
        {
            throw new InvalidOperationException(
                ErrorMessages.Division.ConflictingRosterEnrollment(string.Join(", ", conflictingTeamIds)));
        }
    }
}
