using AutoMapper;
using Club12.Entities.DivisionEntity;
using Club12.Entities.TeamEntity;
using Club12.Services.Divisions;
using Club12.Services.Teams;
using Club12.Viewmodels.Division;
using Club12.Viewmodels.Team;
using Microsoft.AspNetCore.Mvc;

namespace Club12.Controllers;

/// <summary>
/// Controller for managing teams.
/// </summary>
[Route("api/[controller]")]
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
    /// Creates a new team.
    /// </summary>
    /// <param name="teamRequest">The team request.</param>
    /// <returns>The created team response.
    /// <para>Returns 200 (Ok) with the team response if the creation was succesful.</para>
    /// <para>Returns 400 (BadRequest) if the division with the provided id was not found.</para>
    /// </returns>
    [HttpPost("team")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TeamResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<TeamResponse> CreateTeam(TeamRequest teamRequest)
    {
        Guid divisionId = teamRequest.DivisionId;
        Division? existingDivision = _divisionService.GetDivisionById(divisionId);

        if (existingDivision is null)
        {
            return BadRequest($"There is no division with id: {divisionId}.");
        }

        Team mappedTeam = _mapper.Map<Team>(teamRequest);
        Team createdTeam = _teamService.CreateTeam(mappedTeam);
        TeamResponse teamResponse = _mapper.Map<TeamResponse>(createdTeam);

        return CreatedAtAction(nameof(GetTeamById), new { id = createdTeam.Id }, teamResponse);
    }

    /// <summary>
    /// Retrieves a team by its id.
    /// </summary>
    /// <param name="id">The id of the team to retrieve.</param>
    /// <returns>The team with the specified id.
    /// <para>Returns 200 (Ok) with the team response if it was found.</para>
    /// <para>Returns 400 (BadRequest) if the team with the provided id was not found.</para>
    /// </returns>
    [HttpGet("team/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DivisionResponse))]
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
}
