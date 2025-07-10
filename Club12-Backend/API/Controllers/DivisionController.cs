using AutoMapper;

using Entities.DTOs.Abstract;
using Entities.DTOs.Divisions;
using Entities.DTOs.TopScorer;
using Entities.Models.Divisions;
using Entities.Models.PlayoffSeries;
using Entities.Models.Teams;
using Entities.Models.TopScorers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Services.Services.Divisions;
using Services.Services.Matches;
using Services.Services.PlayoffSeries;
using Services.Services.Teams;

namespace Club12.API.Controllers;

/// <summary>
/// Controller for managing divisions.
/// </summary>
/// <param name="_divisionService">The division service.</param>
/// <param name="_teamService">The team service.</param>
/// <param name="_matchService">The match service.</param>
/// <param name="_playoffSeriesService">The playoff series service.</param>
/// <param name="_mapper">The AutoMapper instance.</param>
[Authorize(Roles = "SuperAdmin")]
[Route("api/divisions/")]
[ApiController]
public class DivisionController(
    IDivisionService _divisionService,
    IMatchService _matchService,
    ITeamService _teamService,
    IPlayoffSeriesService _playoffSeriesService,
    IMapper _mapper
    ) : ControllerBase
{

    /// <summary>
    /// Creates a new division.
    /// </summary>
    /// <param name="divisionRequest">The division request.</param>
    /// <returns>The created division response.
    /// <para>Returns 201 (Created) with the division response if the creation was successful.</para>
    /// <para>Returns 403 (Forbidden) if the user is not authenticated.</para>
    /// </returns>
    [HttpPost()]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(DetailedDivisionResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DetailedDivisionResponse>> CreateDivision(CreateDivisionRequest divisionRequest)
    {
        Division mappedDivision = _mapper.Map<Division>(divisionRequest);
        Division createdDivision = await _divisionService.CreateDivisionAsync(mappedDivision);
        DetailedDivisionResponse divisionResponse = _mapper.Map<DetailedDivisionResponse>(createdDivision);

        return new ObjectResult(divisionResponse) { StatusCode = StatusCodes.Status201Created };
    }

    /// <summary>
    /// Retrieves a division by its id.
    /// </summary>
    /// <param name="id">The id of the division to retrieve.</param>
    /// <returns>The division with the specified id.
    /// <para>Returns 200 (Ok) with the division response if it was found.</para>
    /// <para>Returns 400 (Bad Request) if the division with the provided id was not found.</para>
    /// </returns>
    [AllowAnonymous]
    [HttpGet("{id:guid}/detail")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DetailedDivisionResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DetailedDivisionResponse>> GetDivisionById(Guid id)
    {
        Division? division = await _divisionService.GetDivisionWithStatsAsync(id);

        if (division is null)
        {
            return BadRequest($"Division with id {id} not found.");
        }

        DetailedDivisionResponse divisionResponse = _mapper.Map<DetailedDivisionResponse>(division);

        return Ok(divisionResponse);
    }

    /// <summary>
    /// Deletes a division by its id.
    /// </summary>
    /// <param name="id">The id of the division to delete.</param>
    /// <returns>
    /// Returns 200 (Ok) if the division was successfully deleted.
    /// Returns 400 (Bad Request) if the division with the provided id was not found.
    /// Returns 403 (Forbidden) if the user is not authenticated.
    /// </returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteDivisionById(Guid id)
    {
        Division? division = await _divisionService.GetDivisionByIdAsync(id);

        if (division is null)
        {
            return BadRequest($"Division with id {id} not found.");
        }

        bool deleteResult = await _divisionService.DeleteDivisionAsync(division);
        return !deleteResult ? BadRequest("Failed to delete the division.") : NoContent();

    }

    /// <summary>
    /// Updates a division by its id.
    /// </summary>
    /// <param name="id">The id of the division to update.</param>
    /// <param name="divisionRequest">The updated division information.</param>
    /// <returns>
    /// Returns 200 (Ok) with the updated division response if the update was successful.
    /// Returns 400 (Bad Request) if the division with the provided id was not found.
    /// Returns 403 (Forbidden) if the user is not authenticated.
    /// </returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateDivisionById(Guid id, UpdateDivisionRequest divisionRequest)
    {
        Division? existingDivision = await _divisionService.GetDivisionByIdAsync(id);

        if (existingDivision is null)
        {
            return BadRequest($"Division with id {id} not found.");
        }

        _mapper.Map(divisionRequest, existingDivision);
        bool updateResult = await _divisionService.UpdateDivisionAsync(existingDivision);

        return !updateResult ? BadRequest("Failed to update the division.") : NoContent();
    }

    /// <summary>
    /// Retrieves filtered divisions with pagination.
    /// </summary>
    /// <param name="filterRequest">The filtering and pagination parameters.</param>
    /// <returns>A paginated response containing the filtered divisions.</returns>
    [AllowAnonymous]
    [HttpGet()]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<DetailedDivisionResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<DetailedDivisionResponse>>> GetFilteredDivisions([FromQuery] GetDivisionsFilteredRequest filterRequest)
    {
        PaginatedResponse<Division> paginatedDivisions = await _divisionService.GetAllDivisionsAsync(filterRequest);

        PaginatedResponse<DetailedDivisionResponse> response = _mapper.Map<PaginatedResponse<DetailedDivisionResponse>>(paginatedDivisions);

        return Ok(response);
    }

    /// <summary>
    /// Generates the fixture (matches) for the specified division.
    /// </summary>
    /// <param name="id">The ID of the division for which to generate the fixture.</param>
    /// <returns>Returns 200 (Ok) if the fixture is successfully generated.
    /// <para>Returns 400 (Bad Request) if the division with the specified ID does not exist.</para></returns>
    [HttpPost("{id:guid}/generate-fixture")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateFixtureForDivision(Guid id)
    {
        Division? division = await _divisionService.GetDivisionByIdAsync(id);

        if (division is null)
        {
            return BadRequest($"Division with id {id} not found.");
        }

        if (division.IsFinished)
        {
            return BadRequest("Division is already finished.");
        }

        await _matchService.GenerateFixtureAsync(id, [.. division.Teams]);

        return Ok("Fixture generated successfully.");
    }

    /// <summary>
    /// The top scorers for the specified division.
    /// </summary>
    /// <param name="id">The ID of the division to get top scorers for.</param>
    /// <returns>A list of top scorers for the specified division.</returns>
    [AllowAnonymous]
    [HttpGet("{id:guid}/top-scorers")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<TopScorerResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<TopScorerResponse>>> GetTopScorersByDivision(Guid id)
    {
        List<TopScorer>? topScorers = await _divisionService.GetTopScorersByDivisionAsync(id);

        if (topScorers is null)
        {
            return BadRequest($"Division with id {id} not found.");
        }

        List<TopScorerResponse> topScorersResponse = _mapper.Map<List<TopScorerResponse>>(topScorers);

        return Ok(topScorersResponse);
    }

    /// <summary>
    /// Generates the playoff matches (Bo3 rounds) for the specified division.
    /// </summary>
    /// <param name="id">The ID of the division for which to generate the playoffs.</param>
    /// <returns>Returns 200 (Ok) if the playoffs are successfully generated.
    /// <para>Returns 400 (Bad Request) if the division does not exist, playoffs are already generated, or there are insufficient teams to generate playoffs.</para></returns>
    [HttpPost("{id:guid}/generate-playoffs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GeneratePlayoffs(Guid id)
    {
        Division? division = await _divisionService.GetDivisionWithStatsAsync(id);

        if (division is null)
        {
            return BadRequest($"Division with id {id} not found.");
        }

        if (division.PlayoffsGenerated)
        {
            return BadRequest("Playoffs have already been generated for this division.");
        }

        if (division.Teams.Count < 8)
        {
            return BadRequest("Insufficient teams to generate playoffs. At least 8 teams are required.");
        }

        List<Team> teams = division.Teams.Take(8).ToList();

        bool seedsAssigned = await AssignSeedsAndUpdateTeamsAsync(teams);
        if (!seedsAssigned)
        {
            return BadRequest("Failed to update team seeds.");
        }

        List<PlayoffSerie> playoffSeries = await _playoffSeriesService.CreatePlayoffSeriesAsync();
        await _matchService.GeneratePlayoffMatchesAsync(id, teams, playoffSeries);

        division.PlayoffsGenerated = true;
        bool updateResult = await _divisionService.UpdateDivisionAsync(division);

        return !updateResult ? BadRequest("Failed to update division with playoffs generated.") : Ok("Playoffs generated successfully.");
    }

    /// <summary>
    /// Assigns seeds to the top 8 teams and updates them in the database.
    /// </summary>
    /// <param name="teams">The list of teams to assign seeds to.</param>
    /// <returns>True if the teams were successfully updated; otherwise, false.</returns>
    private async Task<bool> AssignSeedsAndUpdateTeamsAsync(List<Team> teams)
    {
        if (teams.Count < 8)
        {
            return false;
        }

        List<Team> seededTeams = teams
            .Select((team, index) =>
            {
                team.Seed = index + 1;
                return team;
            })
            .ToList();

        return await _teamService.UpdateTeamsAsync(seededTeams);
    }
}
