using API.Utils;

using Application.DTOs.Abstract.Response;
using Application.DTOs.Team.Request;
using Application.DTOs.Team.Response;
using Application.Interfaces.Services;
using Application.Utils.Constants;

using AutoMapper;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Storage;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Threading.Tasks;

namespace API.Controllers;

/// <summary>
/// Controller for managing teams. Reads are public; writes require
/// Owner or Admin.
/// </summary>
/// <param name="teamService">The team service for handling team-related operations.</param>
/// <param name="supabaseHelper">The Supabase helper for storage operations.</param>
/// <param name="mapper">The AutoMapper instance for mapping data models.</param>
[Route("api/teams/")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
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
            return BadRequest(ErrorMessages.Media.InvalidImageFile);
        }

        string logoUrl = await supabaseHelper.UploadImageAsync<Team>(teamRequest.LogoFile.OpenReadStream(), teamRequest.LogoFile.FileName);

        Team team = mapper.Map<Team>(teamRequest);
        team.LogoUrl = logoUrl;

        Team createdTeam = await teamService.CreateTeamAsync(team);
        TeamResponse teamResponse = mapper.Map<TeamResponse>(createdTeam);

        return CreatedAtAction(nameof(GetTeamById), new { idOrSlug = createdTeam.Id }, teamResponse);
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
            return BadRequest(ErrorMessages.Media.InvalidImageFile);
        }

        Team? team = await teamService.GetTeamByIdAsync(id);
        if (team is null)
        {
            return this.NotFoundProblem(nameof(Team), id);
        }

        team.LogoUrl = await supabaseHelper.UploadImageAsync<Team>(
            logoRequest.LogoFile.OpenReadStream(),
            logoRequest.LogoFile.FileName);

        await teamService.UpdateTeamAsync(team);
        return Ok();
    }

    /// <summary>
    /// Retrieves a team by its id or its public slug. The embedded player
    /// roster is scoped to one season.
    /// </summary>
    /// <param name="idOrSlug">The id (GUID) or slug of the team to retrieve.</param>
    /// <param name="tournamentId">
    /// Optional: the season whose roster to embed. Defaults to the team's
    /// own current tournament (i.e. today's roster) when omitted — pass this
    /// to look up the roster the team had during a past season instead.
    /// </param>
    /// <returns>The team with the specified id or slug.
    /// <para>Returns 200 (OK) with the team response if it was found.</para>
    /// <para>Returns 404 (Not Found) if the team with the provided id or slug was not found.</para>
    /// </returns>
    [AllowAnonymous]
    [HttpGet("{idOrSlug}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TeamResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamResponse>> GetTeamById(string idOrSlug, [FromQuery] Guid? tournamentId = null)
    {
        Team? team = await teamService.GetTeamByIdOrSlugAsync(idOrSlug, tournamentId);

        if (team is null)
        {
            return this.NotFoundProblem(nameof(Team), idOrSlug);
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
            string[] logoUrlSegments = team.LogoUrl.Split('/');
            await supabaseHelper.DeleteImageAsync<Team>(logoUrlSegments[^1]);
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
