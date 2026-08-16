using AutoMapper;
using API.Utils;
using Application.DTOs.Abstract.Response;
using Application.DTOs.Team.Request;
using Application.DTOs.Team.Response;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities.Models;
using Domain.Enums;
using Application.Utils.Helper.SupabaseHelper;

namespace API.Controllers;

/// <summary>
/// Controller for managing teams. Reads are public; writes require
/// TeamManager, TournamentManager, or Owner.
/// </summary>
/// <param name="teamService">The team service for handling team-related operations.</param>
/// <param name="supabaseHelper">The Supabase helper for storage operations.</param>
/// <param name="mapper">The AutoMapper instance for mapping data models.</param>
[Route("api/teams/")]
[ApiController]
[Authorize(Roles = Roles.TeamManagerOrTournamentManagerOrOwner)]
public class TeamController(
    ITeamService teamService,
    SupabaseHelper supabaseHelper,
    IMapper mapper
    ) : ControllerBase
{
    /// <summary>
    /// Creates a new team.
    /// </summary>
    /// <param name="teamRequest">The team creation request object containing the team details.</param>
    /// <returns>The created team response with details of the new team.</returns>
    [HttpPost()]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TeamResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeamResponse>> CreateTeam(CreateTeamRequest teamRequest)
    {
        if (!teamRequest.LogoFile.IsValidImageFile())
        {
            return BadRequest("The logo file must be a valid JPEG/PNG image.");
        }

        string logoUrl = await supabaseHelper.UploadImageAsync<Team>(teamRequest.LogoFile.OpenReadStream(), teamRequest.LogoFile.FileName);

        Team team = mapper.Map<Team>(teamRequest);
        team.LogoUrl = logoUrl;

        Team createdTeam = await teamService.CreateTeamAsync(team);
        TeamResponse teamResponse = mapper.Map<TeamResponse>(createdTeam);

        return CreatedAtAction(nameof(GetTeamById), new { id = createdTeam.Id }, teamResponse);
    }

    /// <summary>
    /// Updates a team by its id.
    /// </summary>
    /// <param name="id">The id of the team to update.</param>
    /// <param name="teamRequest">The team request excluding logo update.</param>
    /// <returns>
    /// Returns 200 (OK) with the updated team response if the update was successful.
    /// Returns 400 (Bad Request) if the team with the provided id was not found.
    /// Returns 403 (Forbidden) if the user is not authenticated.
    /// </returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdateTeam(Guid id, UpdateTeamRequest teamRequest)
    {
        Team? existingTeam = await teamService.GetTeamByIdAsync(id);

        if (existingTeam is null)
        {
            return this.NotFoundProblem(nameof(Team), id);
        }

        mapper.Map(teamRequest, existingTeam);
        
        await teamService.UpdateTeamAsync(existingTeam);

        return NoContent();
    }

    /// <summary>
    /// Updates the logo of a team.
    /// </summary>
    /// <param name="id">The id of the team to update the logo.</param>
    /// <param name="logoRequest">The update team logo request.</param>
    /// <returns>Returns 200 (OK) if the logo was successfully updated.</returns>
    [HttpPut("{id:guid}/logo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateTeamLogo(Guid id, UpdateTeamLogoRequest logoRequest)
    {
        if (!logoRequest.LogoFile.IsValidImageFile())
        {
            return BadRequest("The logo file must be a valid JPEG/PNG image.");
        }

        Team? team = await teamService.GetTeamByIdAsync(id);
        if (team is null)
        {
            return this.NotFoundProblem(nameof(Team), id);
        }

        await teamService.UpdateTeamAsync(team);
        return Ok();
    }

    /// <summary>
    /// Retrieves a team by its id.
    /// </summary>
    /// <param name="id">The id of the team to retrieve.</param>
    /// <returns>The team with the specified id.
    /// <para>Returns 200 (OK) with the team response if it was found.</para>
    /// <para>Returns 400 (Bad Request) if the team with the provided id was not found.</para>
    /// </returns>
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TeamResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamResponse>> GetTeamById(Guid id)
    {
        Team? team = await teamService.GetTeamByIdAsync(id);

        if (team is null)
        {
            return this.NotFoundProblem(nameof(Team), id);
        }

        TeamResponse teamResponse = mapper.Map<TeamResponse>(team);
        return Ok(teamResponse);
    }

    /// <summary>
    /// Deletes a team by its id.
    /// </summary>
    /// <param name="id">The id of the team to delete.</param>
    /// <returns>
    /// Returns 200 (OK) if the team was successfully deleted.
    /// Returns 400 (Bad Request) if the team with the provided id was not found.
    /// Returns 403 (Forbidden) if the user is not authenticated.
    /// </returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteTeamById(Guid id)
    {
        Team? team = await teamService.GetTeamByIdAsync(id);

        if (team is null)
        {
            return this.NotFoundProblem(nameof(Team), id);
        }

        if (!string.IsNullOrWhiteSpace(team.LogoUrl))
        {
            await supabaseHelper.DeleteImageAsync<Team>(team.LogoUrl.Split('/').Last());
        }
        await teamService.DeleteTeamAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Retrieves filtered teams with pagination.
    /// </summary>
    /// <param name="filterRequest">The filtering and pagination parameters.</param>
    /// <returns>A paginated response containing the filtered teams.</returns>
    [AllowAnonymous]
    [HttpGet()]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<TeamResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<TeamResponse>>> GetFilteredTeams([FromQuery] GetTeamsFilteredRequest filterRequest)
    {
        PaginatedResponse<Team> paginatedTeams = await teamService.GetAllTeamsAsync(filterRequest);

        PaginatedResponse<TeamResponse> response = mapper.Map<PaginatedResponse<TeamResponse>>(paginatedTeams);

        return Ok(response);
    }
}
