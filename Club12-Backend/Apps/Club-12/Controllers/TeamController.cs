using AutoMapper;
using Club12.Entities.DivisionEntity;
using Club12.Entities.TeamEntity;
using Club12.Services.Divisions;
using Club12.Services.Teams;
using Club12.Viewmodels.Team;
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
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="TeamController"/> class.
    /// </summary>
    /// <param name="teamService">The team service.</param>
    /// <param name="divisionService">The division service.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    public TeamController(
        ITeamService teamService,
        IDivisionService divisionService,
        IMapper mapper
    )
    {
        _teamService = teamService;
        _divisionService = divisionService;
        _mapper = mapper;
    }

    /// <summary>
    /// Creates new teams.
    /// </summary>
    /// <param name="teamRequest">The team request.</param>
    /// <returns>The created team response.
    /// <para>Returns 201 (Created) with the team response if the creation was successful.</para>
    /// <para>Returns 400 (Bad Request) if the division with the provided id was not found.</para>
    /// <para>Returns 403 (Forbidden) if the user is not authenticated.</para>
    /// </returns>
    [HttpPost("teams")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TeamResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<TeamResponse> CreateTeam(TeamRequest teamRequest)
    {
        Guid teamId = teamRequest.DivisionId;
        Division? existingDivision = _divisionService.GetDivisionById(teamId);

        if (existingDivision is null)
        {
            return BadRequest($"There is no division with id: {teamId}.");
        }

        Team mappedTeam = _mapper.Map<Team>(teamRequest);
        Team createdTeam = _teamService.CreateTeam(mappedTeam);
        TeamResponse teamResponse = _mapper.Map<TeamResponse>(createdTeam);

        return new ObjectResult(teamResponse) { StatusCode = StatusCodes.Status201Created };
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
    /// Updates a team by its id.
    /// </summary>
    /// <param name="teamId">The id of the team to update.</param>
    /// <param name="teamRequest">The team request.</param>
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
}
