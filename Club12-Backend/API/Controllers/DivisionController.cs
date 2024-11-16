using AutoMapper;

using Entities.DTOs.Abstract;
using Entities.DTOs.Division;
using Entities.DTOs.TopScorer;
using Entities.Models.DivisionEntity;
using Entities.Models.TopScorerModel;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Services.Services.DivisionService;
using Services.Services.MatchService;
using Services.Services.PlayerStatisticService;

namespace Club12.API.Controllers;

/// <summary>
/// Controller for managing divisions.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DivisionController"/> class.
/// </remarks>
/// <param name="_divisionService">The division service.</param>
/// <param name="_matchService">The match service.</param>
/// <param name="_playerStatisticService">The player statistics service.</param>
/// <param name="_mapper">The AutoMapper instance.</param>
[Authorize(Roles = "SuperAdmin")]
[Route("api/divisions/")]
[ApiController]
public class DivisionController(
    IDivisionService _divisionService,
    IMatchService _matchService,
    IPlayerStatisticService _playerStatisticService,
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
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(DivisionResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult CreateDivision(CreateDivisionRequest divisionRequest)
    {
        Division mappedDivision = _mapper.Map<Division>(divisionRequest);
        Division createdDivision = _divisionService.CreateDivision(mappedDivision);
        DivisionResponse divisionResponse = _mapper.Map<DivisionResponse>(createdDivision);

        return new ObjectResult(divisionResponse) { StatusCode = StatusCodes.Status201Created };
    }

    /// <summary>
    /// Retrieves a division by its id.
    /// </summary>
    /// <param name="divisionId">The id of the division to retrieve.</param>
    /// <returns>The division with the specified id.
    /// <para>Returns 200 (Ok) with the division response if it was found.</para>
    /// <para>Returns 400 (Bad Request) if the division with the provided id was not found.</para>
    /// </returns>
    [AllowAnonymous]
    [HttpGet("{divisionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DivisionResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<DivisionResponse> GetDivisionById(Guid divisionId)
    {
        Division? division = _divisionService.GetDivisionWithStats(divisionId);

        if (division is null)
        {
            return BadRequest($"Division with id {divisionId} not found.");
        }

        DivisionResponse divisionResponse = _mapper.Map<DivisionResponse>(division);

        return Ok(divisionResponse);
    }

    /// <summary>
    /// Deletes a division by its id.
    /// </summary>
    /// <param name="divisionId">The id of the division to delete.</param>
    /// <returns>
    /// Returns 200 (Ok) if the division was successfully deleted.
    /// Returns 400 (Bad Request) if the division with the provided id was not found.
    /// Returns 403 (Forbidden) if the user is not authenticated.
    /// </returns>
    [HttpDelete("{divisionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult DeleteDivisionById(Guid divisionId)
    {
        Division? division = _divisionService.GetDivisionById(divisionId);

        if (division is null)
        {
            return BadRequest($"Division with id {divisionId} not found.");
        }

        _divisionService.DeleteDivision(division);
        return Ok();
    }

    /// <summary>
    /// Updates a division by its id.
    /// </summary>
    /// <param name="divisionId">The id of the division to update.</param>
    /// <param name="divisionRequest">The updated division information.</param>
    /// <returns>
    /// Returns 200 (Ok) with the updated division response if the update was successful.
    /// Returns 400 (Bad Request) if the division with the provided id was not found.
    /// Returns 403 (Forbidden) if the user is not authenticated.
    /// </returns>
    [HttpPut("{divisionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DivisionResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateDivisionById(Guid divisionId, UpdateDivisionRequest divisionRequest)
    {
        Division? existingDivision = _divisionService.GetDivisionById(divisionId);

        if (existingDivision is null)
        {
            return BadRequest($"Division with id {divisionId} not found.");
        }

        _mapper.Map(divisionRequest, existingDivision);
        bool updateResult = await _divisionService.UpdateDivisionAsync(existingDivision);

        return !updateResult ? BadRequest("Failed to update the division.") : Ok();
    }

    /// <summary>
    /// Retrieves filtered divisions with pagination.
    /// </summary>
    /// <param name="filterRequest">The filtering and pagination parameters.</param>
    /// <returns>A paginated response containing the filtered divisions.</returns>
    [AllowAnonymous]
    [HttpGet()]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<DivisionResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<DivisionResponse>>> GetFilteredDivisions([FromQuery] GetDivisionsFilteredRequest filterRequest)
    {
        PaginatedResponse<Division> paginatedDivisions = await _divisionService.GetAllDivisionsAsync(filterRequest);

        PaginatedResponse<DivisionResponse> response = _mapper.Map<PaginatedResponse<DivisionResponse>>(paginatedDivisions);

        return Ok(response);
    }

    /// <summary>
    /// Generates the fixture (matches) for the specified division.
    /// </summary>
    /// <param name="divisionId">The ID of the division for which to generate the fixture.</param>
    /// <returns>Returns 200 (Ok) if the fixture is successfully generated.
    /// <para>Returns 400 (Bad Request) if the division with the specified ID does not exist.</para></returns>
    [HttpPost("{divisionId:guid}/generate-fixture")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateFixtureForDivision(Guid divisionId)
    {
        Division? division = _divisionService.GetDivisionById(divisionId);

        if (division is null)
        {
            return BadRequest($"Division with id {divisionId} not found.");
        }

        if (division.IsFinished)
        {
            return BadRequest("Division is already finished.");
        }

        await _matchService.GenerateFixtureAsync([.. division.Teams], divisionId);

        return Ok("Fixture generated successfully.");
    }

    /// <summary>
    /// The top scorers for the specified division.
    /// </summary>
    /// <param name="divisionId">The ID of the division to get top scorers for.</param>
    /// <returns>A list of top scorers for the specified division.</returns>
    [AllowAnonymous]
    [HttpGet("top-scorers/{divisionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<TopScorerResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<List<TopScorerResponse>> GetTopScorersByDivision(Guid divisionId)
    {
        Division? division = _divisionService.GetDivisionById(divisionId);

        if (division is null)
        {
            return BadRequest($"Division with id {divisionId} not found.");
        }

        List<Guid> matchIds = division.Matches.Select(match => match.Id).ToList();
        int totalMatches = (division.Teams.Count * 2) - 1;
        List<TopScorer> topScorers = _playerStatisticService.GetTopScorersByDivision(matchIds, totalMatches);

        List<TopScorerResponse> topScorersResponse = _mapper.Map<List<TopScorerResponse>>(topScorers);

        return Ok(topScorersResponse);
    }

}


