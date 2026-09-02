using API.Utils;

using Application.DTOs.TeamStaff.Request;
using Application.DTOs.TeamStaff.Response;
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
/// Manages a team's technical staff (cuerpo técnico — DT, Asistente),
/// scoped per team + tournament (season). Creating and deleting
/// require Admin or Owner; listing is public so a team's profile can show its
/// staff.
/// </summary>
/// <param name="staffService">The team-staff service.</param>
/// <param name="teamService">The team service, used to validate the team.</param>
/// <param name="tournamentService">The tournament service, used to validate the tournament.</param>
/// <param name="mapper">The AutoMapper instance.</param>
[Route("api/")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
public class TeamStaffController(
    ITeamStaffService staffService,
    ITeamService teamService,
    ITournamentService tournamentService,
    IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Adds a member to a team's technical staff for a given tournament.
    /// </summary>
    /// <param name="teamId">The team the staff member belongs to.</param>
    /// <param name="request">The staff member (full name, role, tournament).</param>
    /// <returns>
    /// 201 (Created) with the staff member; 404 (Not Found) when the team or
    /// tournament does not exist; 403 (Forbidden) for non-staff callers.
    /// </returns>
    [HttpPost("teams/{teamId:guid}/staff")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TeamStaffResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TeamStaffResponse>> CreateTeamStaff(
        Guid teamId, CreateTeamStaffRequest request)
    {
        Team? team = await teamService.GetTeamByIdAsync(teamId);
        if (team is null)
        {
            return this.NotFoundProblem(nameof(Team), teamId);
        }

        Tournament? tournament = await tournamentService.GetTournamentByIdAsync(request.TournamentId);
        if (tournament is null)
        {
            return this.NotFoundProblem(nameof(Tournament), request.TournamentId);
        }

        TeamStaff staff = mapper.Map<TeamStaff>(request);
        staff.TeamId = teamId;

        TeamStaff created = await staffService.CreateAsync(staff);
        TeamStaffResponse response = mapper.Map<TeamStaffResponse>(created);

        return new ObjectResult(response) { StatusCode = StatusCodes.Status201Created };
    }

    /// <summary>
    /// Lists a team's technical staff for a given tournament. Public so a
    /// team's profile can show its staff.
    /// </summary>
    /// <param name="teamId">The team whose staff to list.</param>
    /// <param name="tournamentId">The tournament (season) to scope the staff to.</param>
    /// <returns>200 (OK) with the staff (in the order they were added).</returns>
    [AllowAnonymous]
    [HttpGet("teams/{teamId:guid}/staff")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<TeamStaffResponse>))]
    public async Task<ActionResult<List<TeamStaffResponse>>> GetTeamStaff(
        Guid teamId, [FromQuery] Guid tournamentId)
    {
        List<TeamStaff> staff =
            await staffService.GetByTeamAndTournamentAsync(teamId, tournamentId);
        return Ok(mapper.Map<List<TeamStaffResponse>>(staff));
    }

    /// <summary>
    /// Removes a staff member by their id.
    /// </summary>
    /// <param name="id">The id of the staff member to remove.</param>
    /// <returns>204 (No Content); 403 (Forbidden) for non-staff callers.</returns>
    [HttpDelete("staff/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteTeamStaff(Guid id)
    {
        await staffService.DeleteAsync(id);
        return NoContent();
    }
}
