using AutoMapper;
using Club12.DTOs.Team;
using Club12.Entities.DivisionEntity;
using Club12.Entities.TeamEntity;
using Club12.Services.Divisions;
using Club12.Services.Teams;
using Club12.Services.Utils.Cloudfare;
using Club12.Utils.Controller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Club12.Controllers;

/// <summary>
/// Controller for managing teams.
/// </summary>
[Authorize(Roles = "SuperAdmin, Admin")]
[Route("api/")]
[ApiController]
public class TeamController : ControllerBase
{
    private readonly ITeamService _teamService;
    private readonly IDivisionService _divisionService;
    private readonly ICloudflareService _cloudflareService;
    private readonly IControllerUtils _controllerUtils;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="TeamController"/> class.
    /// </summary>
    /// <param name="teamService">The team service.</param>
    /// <param name="divisionService">The division service.</param>
    /// <param name="controllerUtils">Controller utils that allow us to get user data from requests.</param>
    /// <param name="cloudflareService">The Cloudflare service.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    public TeamController(
        ITeamService teamService,
        IDivisionService divisionService,
        IControllerUtils controllerUtils,
        ICloudflareService cloudflareService,
        IMapper mapper
    )
    {
        _teamService = teamService;
        _divisionService = divisionService;
        _controllerUtils = controllerUtils;
        _cloudflareService = cloudflareService;
        _mapper = mapper;
    }

    /// <summary>
    /// Creates a new team.
    /// </summary>
    /// <param name="teamRequest">The team creation request object containing the team details.</param>
    /// <returns>The created team response with details of the new team.</returns>
    [HttpPost("teams")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TeamResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeamResponse>> CreateTeam(TeamRequest teamRequest)
    {
        if (!IsValidImageFile(teamRequest.LogoFile))
        {
            return BadRequest("The logo file must be a valid JPEG/PNG image.");
        }

        Division? division = _divisionService.GetDivisionById(teamRequest.DivisionId);
        if (division is null)
        {
            return BadRequest($"Division with id {teamRequest.DivisionId} not found.");
        }

        string logoUrl = await _cloudflareService.UploadLogoAsync(teamRequest.LogoFile.OpenReadStream(), teamRequest.LogoFile.FileName);

        Team team = _mapper.Map<Team>(teamRequest);
        team.LogoUrl = logoUrl;

        Team createdTeam = _teamService.CreateTeam(team);
        TeamResponse teamResponse = _mapper.Map<TeamResponse>(createdTeam);

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
    public async Task<ActionResult> UpdateTeam(Guid teamId, TeamRequest teamRequest)
    {
        Team? existingTeam = _teamService.GetTeamById(teamId);

        if (existingTeam is null)
        {
            return BadRequest($"Team with id {teamId} not found.");
        }

        _mapper.Map(teamRequest, existingTeam);
        bool updateResult = await _teamService.UpdateTeam(existingTeam);

        return !updateResult ? BadRequest("Failed to update the team.") : Ok();
    }

    /// <summary>
    /// Updates the logo of a team.
    /// </summary>
    /// <param name="teamId">The id of the team to update the logo.</param>
    /// <param name="logoFile">The new logo file.</param>
    /// <returns>Returns 200 (OK) if the logo was successfully updated.</returns>
    [HttpPut("teams/{teamId:guid}/logo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateTeamLogo(Guid teamId, [FromForm] IFormFile logoFile)
    {
        if (logoFile is null || !IsValidImageFile(logoFile))
        {
            return BadRequest("The logo file must be a valid JPEG/PNG image.");
        }

        Team? team = _teamService.GetTeamById(teamId);
        if (team is null)
        {
            return BadRequest($"Team with id {teamId} not found.");
        }

        string logoUrl = await _cloudflareService.UploadLogoAsync(logoFile.OpenReadStream(), logoFile.FileName);
        team.LogoUrl = logoUrl;

        bool updateResult = await _teamService.UpdateTeam(team);
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
        Team? team = _teamService.GetTeamById(id);

        if (team is null)
        {
            return BadRequest($"Team with id {id} not found.");
        }

        TeamResponse teamResponse = _mapper.Map<TeamResponse>(team);
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
        Team? team = _teamService.GetTeamById(id);

        if (team is null)
        {
            return BadRequest($"Team with id {id} not found.");
        }

        _teamService.DeleteTeam(team);
        return Ok();
    }

    /// <summary>
    /// Checks if the provided file is a valid image (JPEG or PNG).
    /// </summary>
    /// <param name="file">The uploaded file to check.</param>
    /// <returns>True if the file is a valid image, otherwise false.</returns>
    private static bool IsValidImageFile(IFormFile file)
    {
        string[] validExtensions = { ".jpg", ".jpeg", ".png" };
        string fileExtension = Path.GetExtension(file.FileName).ToLower();

        return validExtensions.Contains(fileExtension);
    }
}
