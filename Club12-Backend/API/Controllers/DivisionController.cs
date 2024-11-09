using AutoMapper;

using Entities.DTOs.Abstract;
using Entities.DTOs.Division;
using Entities.Models.DivisionEntity;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Services.Services.DivisionService;
using Services.Services.MatchService;

namespace Club12.API.Controllers;

/// <summary>
/// Controller for managing divisions.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DivisionController"/> class.
/// </remarks>
/// <param name="_divisionService">The division service.</param>
/// <param name="_matchService">The match service.</param>
/// <param name="_mapper">The AutoMapper instance.</param>
[Authorize(Roles = "SuperAdmin")]
[Route("api/divisions/")]
[ApiController]
public class DivisionController(
    IDivisionService _divisionService,
    IMatchService _matchService,
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
        Division? division = _divisionService.GetDivisionById(divisionId);

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

        await _matchService.GenerateFixtureAsync(division);

        return Ok("Fixture generated successfully.");
    }

    /// <summary>
    /// Retrieves the positions table for a specified division.
    /// </summary>
    /// <param name="divisionId">The ID of the division.</param>
    /// <returns>The positions table with team statistics for the division.</returns>
    [HttpGet("{divisionId:guid}/positions")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<PositionResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<PositionResponse>>> GetPositionsTable(Guid divisionId)
    {
        Division? division = _divisionService.GetDivisionById(divisionId);

        if (division is null)
        {
            return BadRequest($"Division with id {divisionId} not found.");
        }

        List<PositionResponse> positions = await _matchService.GetPositionsTableAsync(divisionId);

        return Ok(positions);
    }

}


