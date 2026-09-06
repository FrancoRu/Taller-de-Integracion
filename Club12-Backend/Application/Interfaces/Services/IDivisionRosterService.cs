using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

public interface IDivisionRosterService
{
    /// <summary>
    /// Returns every team currently enrolled in the division, independent of any stage placement.
    /// </summary>
    Task<List<Team>> GetRosterAsync(Guid divisionId);

    /// <summary>
    /// Enrolls teams in a division, skipping already-registered teams and rejecting a conflicting cross-division registration.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown as a 409 when a team already holds a conflicting registration, or the tournament has already started.
    /// </exception>
    Task<List<DivisionTeamRegistration>> EnrollTeamsAsync(Guid divisionId, List<Guid> teamIds);

    /// <summary>
    /// Removes teams from a division's roster, cascading to delete their stage placements in that division first.
    /// </summary>
    Task UnenrollTeamsAsync(Guid divisionId, List<Guid> teamIds);

    /// <summary>
    /// Replaces a division's sub-group stages with a new count, re-balancing the untouched roster across them.
    /// </summary>
    Task<List<Stage>> RebuildSubGroupsAsync(Guid divisionId, int subGroupCount);

    /// <summary>
    /// Clears every current sub-group placement and re-deals the whole roster in a fresh balanced distribution.
    /// </summary>
    Task AutoDistributeRosterAsync(Guid divisionId);

    /// <summary>
    /// Manually moves one enrolled team from one sub-group to another, re-validating only the minimum sub-group size.
    /// </summary>
    Task ReassignTeamToSubGroupAsync(Guid teamId, Guid fromStageId, Guid toStageId);
}
