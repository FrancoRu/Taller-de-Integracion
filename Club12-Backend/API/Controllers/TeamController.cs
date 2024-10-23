using AutoMapper;
using Club12.API.Utils;
using Entities.DTOs.Abstract;
using Entities.DTOs.Player;
using Entities.DTOs.Team;
using Entities.Models.DivisionEntity;
using Entities.Models.PlayerEntity;
using Entities.Models.TeamEntity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Services.DivisionService;
using Services.Services.PlayerService;
using Services.Services.TeamService;
using Services.Utils.Cloudfare;
using Services.Utils.Excel;

namespace Club12.API.Controllers;

/// <summary>
/// Controller for managing teams.
/// </summary>
[Authorize(Roles = "SuperAdmin")]
[Route("api/teams/")]
[ApiController]
public class TeamController(
    ITeamService _teamService,
    IDivisionService _divisionService,
    ICloudflareService _cloudflareService,
    IExcelService _excelService,
    IPlayerService _playerService,
    IMapper _mapper
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
    [HttpPut("{teamId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TeamResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdateTeam(Guid teamId, UpdateTeamRequest teamRequest)
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
    /// <param name="logoRequest">The update team logo request.</param>
    /// <returns>Returns 200 (OK) if the logo was successfully updated.</returns>
    [HttpPut("{teamId:guid}/logo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateTeamLogo(Guid teamId, UpdateTeamLogoRequest logoRequest)
    {
        if (!logoRequest.LogoFile.IsValidImageFile())
        {
            return BadRequest("The logo file must be a valid JPEG/PNG image.");
        }

        Team? team = _teamService.GetTeamById(teamId);
        if (team is null)
        {
            return BadRequest($"Team with id {teamId} not found.");
        }

        string logoUrl = await _cloudflareService.UploadLogoAsync(logoRequest.LogoFile.OpenReadStream(), logoRequest.LogoFile.FileName);
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
    [HttpGet("{id:guid}")]
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
    [HttpDelete("{id:guid}")]
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
        PaginatedResponse<Team> paginatedTeams = await _teamService.GetTeamsAsync(filterRequest);

        PaginatedResponse<TeamResponse> response = _mapper.Map<PaginatedResponse<TeamResponse>>(paginatedTeams);

        return Ok(response);
    }

    /// <summary>
    /// Creates a new team with division ID, logo file, and an Excel file containing player data.
    /// </summary>
    /// <param name="batchTeamRequest">The team creation request object containing the team details.</param>
    /// <returns>The created team response with details of the new team.</returns>
    [HttpPost("batch")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TeamResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeamResponse>> CreateTeamWithExcel(BatchCreateTeamRequest batchTeamRequest)
    {
        if (!batchTeamRequest.TeamFile.IsValidExcelFile())
        {
            return BadRequest("The uploaded file must be a valid Excel file (XLSX or XLS).");
        }

        if (!batchTeamRequest.LogoFile.IsValidImageFile())
        {
            return BadRequest("The logo file must be a valid JPEG/PNG image.");
        }

        Division? division = _divisionService.GetDivisionById(batchTeamRequest.DivisionId);
        if (division is null)
        {
            return BadRequest($"Division with id {batchTeamRequest.DivisionId} not found.");
        }

        (string teamName, string threeLetterCode, List<(string FirstName, string SecondName, string LastName, string DocumentNumber)> players) = await _excelService.ReadTeamAndPlayersAsync(batchTeamRequest.TeamFile);

        string logoUrl = await _cloudflareService.UploadLogoAsync(batchTeamRequest.LogoFile.OpenReadStream(), batchTeamRequest.LogoFile.FileName);

        CreateTeamRequest teamRequest = new()
        {
            Name = teamName,
            ThreeLetterCode = threeLetterCode,
            DivisionId = division.Id,
            LogoFile = batchTeamRequest.LogoFile
        };

        Team teamMapped = _mapper.Map<Team>(teamRequest);
        teamMapped.LogoUrl = logoUrl;
        Team createdTeam = _teamService.CreateTeam(teamMapped);

        foreach ((string FirstName, string SecondName, string LastName, string DocumentNumber) in players)
        {
            CreatePlayerRequest playerRequest = new()
            {
                FirstName = FirstName,
                SecondName = SecondName,
                LastName = LastName,
                DocumentNumber = DocumentNumber,
                TeamId = createdTeam.Id
            };

            Player mappedPlayer = _mapper.Map<Player>(playerRequest);
            _playerService.CreatePlayer(mappedPlayer);
        }

        TeamResponse teamResponse = _mapper.Map<TeamResponse>(createdTeam);

        return CreatedAtAction(nameof(GetTeamById), new { id = teamResponse.Id }, teamResponse);
    }
}
