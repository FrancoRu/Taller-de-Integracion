using AutoMapper;
using Club12.Entities.DivisionEntity;
using Club12.Entities.TeamEntity;
using Club12.Extensions;
using Club12.Services.DTOs.Abstract;
using Club12.Services.DTOs.Team;
using Club12.Services.Services.DivisionService;
using Club12.Services.Services.TeamService;
using Club12.Services.Utils.Cloudfare;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Club12.Controllers;

/// <summary>
/// Controller for managing teams.
/// </summary>
[Authorize(Roles = "SuperAdmin")]
[Route("api/")]
[ApiController]
public class TeamController(
    ITeamService teamService,
    IDivisionService divisionService,
    ICloudflareService cloudflareService,
    IMapper mapper
    ) : ControllerBase
{
    /// <summary>
    /// Creates a new team.
    /// </summary>
    /// <param name="teamRequest">The team creation request object containing the team details.</param>
    /// <returns>The created team response with details of the new team.</returns>
    [HttpPost("teams")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TeamResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeamResponse>> CreateTeam(CreateTeamRequest teamRequest)
    {
        if (!teamRequest.LogoFile.IsValidImageFile())
        {
            return BadRequest("The logo file must be a valid JPEG/PNG image.");
        }

        Division? division = divisionService.GetDivisionById(teamRequest.DivisionId);
        if (division is null)
        {
            return BadRequest($"Division with id {teamRequest.DivisionId} not found.");
        }

        string logoUrl = await cloudflareService.UploadLogoAsync(teamRequest.LogoFile.OpenReadStream(), teamRequest.LogoFile.FileName);

        Team team = mapper.Map<Team>(teamRequest);
        Team createdTeam = teamService.CreateTeam(team);
        TeamResponse teamResponse = mapper.Map<TeamResponse>(createdTeam);

        return CreatedAtAction(nameof(GetTeamById), new { id = teamResponse.Id }, teamResponse);
    }

    /// <summary>
    /// Updates a team by its id.
    /// </summary>
    /// <param name="teamId">The id of the team to update.</param>
    /// <param name="teamRequest">The team request excluding logo update.</param>
    /// <returns>
    /// Returns 200 (OK) with the updated team response if the update was successful.
    /// Returns 400 (Bad Request) if the team with the provided id was not found.
    /// Returns 403 (Forbidden) if the user is not authenticated.
    /// </returns>
    [HttpPut("teams/{teamId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TeamResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdateTeam(Guid teamId, UpdateTeamRequest teamRequest)
    {
        Team? existingTeam = teamService.GetTeamById(teamId);

        if (existingTeam is null)
        {
            return BadRequest($"Team with id {teamId} not found.");
        }

        mapper.Map(teamRequest, existingTeam);
        bool updateResult = await teamService.UpdateTeam(existingTeam);

        return !updateResult ? BadRequest("Failed to update the team.") : Ok();
    }

    /// <summary>
    /// Updates the logo of a team.
    /// </summary>
    /// <param name="teamId">The id of the team to update the logo.</param>
    /// <param name="logoRequest">The update team logo request.</param>
    /// <returns>Returns 200 (OK) if the logo was successfully updated.</returns>
    [HttpPut("teams/{teamId:guid}/logo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateTeamLogo(Guid teamId, UpdateTeamLogoRequest logoRequest)
    {
        if (!logoRequest.LogoFile.IsValidImageFile())
        {
            return BadRequest("The logo file must be a valid JPEG/PNG image.");
        }

        Team? team = teamService.GetTeamById(teamId);
        if (team is null)
        {
            return BadRequest($"Team with id {teamId} not found.");
        }

        string logoUrl = await cloudflareService.UploadLogoAsync(logoRequest.LogoFile.OpenReadStream(), logoRequest.LogoFile.FileName);
        team.LogoUrl = logoUrl;

        bool updateResult = await teamService.UpdateTeam(team);
        return !updateResult ? BadRequest("Failed to update the logo.") : Ok();
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
    [HttpGet("teams/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TeamResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<TeamResponse> GetTeamById(Guid id)
    {
        Team? team = teamService.GetTeamById(id);

        if (team is null)
        {
            return BadRequest($"Team with id {id} not found.");
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
    [HttpDelete("teams/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult DeleteTeamById(Guid id)
    {
        Team? team = teamService.GetTeamById(id);

        if (team is null)
        {
            return BadRequest($"Team with id {id} not found.");
        }

        teamService.DeleteTeam(team);
        return Ok();
    }

    /// <summary>
    /// Retrieves filtered teams with pagination.
    /// </summary>
    /// <param name="filterRequest">The filtering and pagination parameters.</param>
    /// <returns>A paginated response containing the filtered teams.</returns>
    [AllowAnonymous]
    [HttpGet("teams")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<TeamResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<TeamResponse>>> GetFilteredTeams([FromQuery] GetTeamsFilteredRequest filterRequest)
    {
        PaginatedResponse<Team> paginatedTeams = await teamService.GetTeamsAsync(filterRequest);

        PaginatedResponse<TeamResponse> response = new()
        {
            Page = paginatedTeams.Page,
            PageSize = paginatedTeams.PageSize,
            TotalCount = paginatedTeams.TotalCount,
            Items = mapper.Map<List<TeamResponse>>(paginatedTeams.Items)
        };

        return Ok(response);
    }
}
