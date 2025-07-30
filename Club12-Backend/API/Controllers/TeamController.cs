using AutoMapper;

using Club12.API.Utils;

using Entities.DTOs.Abstract;
using Entities.DTOs.Team;
using Entities.Models.Teams;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Services.Services.Divisions;
using Services.Services.Teams;
using Services.Utils.Cloudfare;

namespace Club12.API.Controllers;

/// <summary>
/// Controller for managing teams.
/// <param name="_teamService">The team service for handling team-related operations.</param>
/// <param name="_divisionService">The division service for managing divisions associated with teams.</param>
/// <param name="_cloudflareService">The Cloudflare service for handling file uploads.</param>
/// <param name="_mapper">The AutoMapper instance for mapping data models.</param>
/// </summary>
//[Authorize(Roles = "SuperAdmin")]
[Route("api/teams/")]
[ApiController]
public class TeamController(
    ITeamService _teamService,
    IDivisionService _divisionService,
    ICloudflareService _cloudflareService,
    SupabaseHelper _supabaseHelper,
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


        await _supabaseHelper.UploadImageAsync(teamRequest.LogoFile.OpenReadStream(), teamRequest.LogoFile.FileName);

        //string logoUrl = await _cloudflareService.UploadFileAsync(teamRequest.LogoFile.OpenReadStream(), teamRequest.LogoFile.FileName);
        string logoUrl = _supabaseHelper.GetPublicUrl(teamRequest.LogoFile.FileName);
        Team team = _mapper.Map<Team>(teamRequest);
        team.LogoUrl = logoUrl;

        Team createdTeam = await _teamService.CreateTeamAsync(team);
        TeamResponse teamResponse = _mapper.Map<TeamResponse>(createdTeam);

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
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdateTeam(Guid id, UpdateTeamRequest teamRequest)
    {
        Team? existingTeam = await _teamService.GetTeamByIdAsync(id);

        if (existingTeam is null)
        {
            return BadRequest($"Team with id {id} not found.");
        }

        _mapper.Map(teamRequest, existingTeam);
        bool updateResult = await _teamService.UpdateTeamAsync(existingTeam);

        return !updateResult ? BadRequest("Failed to update the team.") : NoContent();
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

        Team? team = await _teamService.GetTeamByIdAsync(id);
        if (team is null)
        {
            return BadRequest($"Team with id {id} not found.");
        }

        string logoUrl = await _cloudflareService.UploadFileAsync(logoRequest.LogoFile.OpenReadStream(), logoRequest.LogoFile.FileName);
        team.LogoUrl = logoUrl;

        bool updateResult = await _teamService.UpdateTeamAsync(team);
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
    public async Task<ActionResult<TeamResponse>> GetTeamById(Guid id)
    {
        Team? team = await _teamService.GetTeamByIdAsync(id);

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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteTeamById(Guid id)
    {
        Team? team = await _teamService.GetTeamByIdAsync(id);

        if (team is null)
        {
            return BadRequest($"Team with id {id} not found.");
        }

        if (!string.IsNullOrWhiteSpace(team.LogoUrl))
        {
            await _supabaseHelper.DeleteImageAsync(team.LogoUrl);
        }

        bool deleteResult = await _teamService.DeleteTeamAsync(team);
        return !deleteResult ? BadRequest("Failed to delete the team.") : NoContent();
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
        PaginatedResponse<Team> paginatedTeams = await _teamService.GetAllTeamsAsync(filterRequest);

        PaginatedResponse<TeamResponse> response = _mapper.Map<PaginatedResponse<TeamResponse>>(paginatedTeams);

        return Ok(response);
    }

    ///// <summary>
    ///// Creates a new team with division ID, logo file, and an Excel file containing player data.
    ///// </summary>
    ///// <param name="batchTeamRequest">The team creation request object containing the team details.</param>
    ///// <returns>The created team response with details of the new team.</returns>
    //[HttpPost("batch")]
    //[ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TeamResponse))]
    //[ProducesResponseType(StatusCodes.Status400BadRequest)]
    //public async Task<ActionResult<TeamResponse>> CreateTeamWithExcel(BatchCreateTeamRequest batchTeamRequest)
    //{
    //    if (!batchTeamRequest.TeamFile.IsValidExcelFile())
    //    {
    //        return BadRequest("The uploaded file must be a valid Excel file (XLSX or XLS).");
    //    }

    //    if (!batchTeamRequest.LogoFile.IsValidImageFile())
    //    {
    //        return BadRequest("The logo file must be a valid JPEG/PNG image.");
    //    }

    //    Divisions? division = _divisionService.GetDivisionById(batchTeamRequest.DivisionId);
    //    if (division is null)
    //    {
    //        return BadRequest($"Divisions with id {batchTeamRequest.DivisionId} not found.");
    //    }

    //    (string teamName, string threeLetterCode, List<(string FirstName, string SecondName, string LastName, string DocumentNumber)> players) = await _excelService.ReadTeamAndPlayersAsync(batchTeamRequest.TeamFile);

    //    string logoUrl = await _cloudflareService.UploadFileAsync(batchTeamRequest.LogoFile.OpenReadStream(), batchTeamRequest.LogoFile.FileName);

    //    CreateTeamRequest teamRequest = new()
    //    {
    //        Name = teamName,
    //        ThreeLetterCode = threeLetterCode,
    //        ShirtColor = "Blue",
    //        DivisionId = division.Id,
    //        LogoFile = batchTeamRequest.LogoFile
    //    };

    //    Team teamMapped = _mapper.Map<Team>(teamRequest);
    //    teamMapped.LogoUrl = logoUrl;
    //    Team createdTeam = _teamService.CreateTeam(teamMapped);

    //    foreach ((string FirstName, string SecondName, string LastName, string DocumentNumber) in players)
    //    {
    //        CreatePlayerRequest playerRequest = new()
    //        {
    //            FirstName = FirstName,
    //            SecondName = SecondName,
    //            LastName = LastName,
    //            DocumentNumber = DocumentNumber,
    //            TeamId = createdTeam.Id
    //        };

    //        Player mappedPlayer = _mapper.Map<Player>(playerRequest);
    //        _playerService.CreatePlayer(mappedPlayer);
    //    }

    //    TeamResponse teamResponse = _mapper.Map<TeamResponse>(createdTeam);

    //    return CreatedAtAction(nameof(GetTeamById), new { id = teamResponse.Id }, teamResponse);
    //}
}
