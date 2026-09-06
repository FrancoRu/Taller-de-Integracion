using Application.DTOs.Divisions.Request;
using Application.DTOs.Stage.Response;
using Application.DTOs.Team.Response;
using Application.Interfaces.Services;

using AutoMapper;

using Domain.Entities.Models;
using Domain.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Controllers;

/// <summary>
/// Manages a division's team roster and its sub-group structure; every route requires Owner or Admin.
/// </summary>
/// <param name="divisionRosterService">Service for the division-level team roster and its sub-group structure.</param>
/// <param name="mapper">AutoMapper instance for mapping between entities and DTOs.</param>
[Route("api/divisions/{divisionId:guid}/")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
public class DivisionRosterController(
    IDivisionRosterService divisionRosterService,
    IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Lists every team currently enrolled in the division, independent of any stage placement.
    /// </summary>
    [HttpGet("roster")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<TeamResponse>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<TeamResponse>>> GetRoster(Guid divisionId)
    {
        List<Team> roster = await divisionRosterService.GetRosterAsync(divisionId);
        return Ok(mapper.Map<List<TeamResponse>>(roster));
    }

    /// <summary>
    /// Enrolls one or more teams in the division's roster, returning the roster as it stands afterward.
    /// </summary>
    [HttpPost("roster")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<TeamResponse>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<List<TeamResponse>>> EnrollTeams(Guid divisionId, EnrollTeamsRequest request)
    {
        await divisionRosterService.EnrollTeamsAsync(divisionId, request.TeamIds);
        List<Team> roster = await divisionRosterService.GetRosterAsync(divisionId);
        return Ok(mapper.Map<List<TeamResponse>>(roster));
    }

    /// <summary>
    /// Removes one or more teams from the division's roster, cascading to their current stage placements first.
    /// </summary>
    [HttpDelete("roster")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> UnenrollTeams(Guid divisionId, UnenrollTeamsRequest request)
    {
        await divisionRosterService.UnenrollTeamsAsync(divisionId, request.TeamIds);
        return NoContent();
    }

    /// <summary>
    /// Replaces the division's sub-group stages with a new count, re-balancing the untouched roster across them.
    /// </summary>
    [HttpPost("sub-groups/rebuild")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<StageResponse>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<List<StageResponse>>> RebuildSubGroups(Guid divisionId, RebuildSubGroupsRequest request)
    {
        List<Stage> stages = await divisionRosterService.RebuildSubGroupsAsync(divisionId, request.SubGroupCount);
        return Ok(mapper.Map<List<StageResponse>>(stages));
    }

    /// <summary>
    /// Clears every current sub-group placement and re-deals the whole roster in a fresh balanced distribution.
    /// </summary>
    [HttpPost("roster/auto-distribute")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> AutoDistributeRoster(Guid divisionId)
    {
        await divisionRosterService.AutoDistributeRosterAsync(divisionId);
        return NoContent();
    }

    /// <summary>
    /// Manually moves one enrolled team from one sub-group to another, re-validating only the minimum sub-group size.
    /// </summary>
    [HttpPost("sub-groups/reassign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> ReassignTeamToSubGroup(Guid divisionId, ReassignTeamToSubGroupRequest request)
    {
        await divisionRosterService.ReassignTeamToSubGroupAsync(request.TeamId, request.FromStageId, request.ToStageId);
        return NoContent();
    }
}
