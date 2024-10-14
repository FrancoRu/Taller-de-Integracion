using AutoMapper;
using Club12.DTOs.Division;
using Club12.Entities.DivisionEntity;
using Club12.Services.Divisions;
using Club12.Viewmodels.Division;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Club12.Controllers;

/// <summary>
/// Controller for managing divisions.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DivisionController"/> class.
/// </remarks>
/// <param name="divisionService">The division service.</param>
/// <param name="mapper">The AutoMapper instance.</param>
[Authorize(Roles = "SuperAdmin")]
[Route("api/")]
[ApiController]
public class DivisionController(
    IDivisionService divisionService,
    IMapper mapper
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
    [HttpPost("divisions")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(DivisionResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult CreateDivision(DivisionRequest divisionRequest)
    {
        Division mappedDivision = mapper.Map<Division>(divisionRequest);
        Division createdDivision = divisionService.CreateDivision(mappedDivision);
        DivisionResponse divisionResponse = mapper.Map<DivisionResponse>(createdDivision);

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
    [HttpGet("divisions/{divisionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DivisionResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<DivisionResponse> GetDivisionById(Guid divisionId)
    {
        Division? division = divisionService.GetDivisionById(divisionId);

        if (division is null)
        {
            return BadRequest($"Division with id {divisionId} not found.");
        }

        DivisionResponse divisionResponse = mapper.Map<DivisionResponse>(division);
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
    [HttpDelete("divisions/{divisionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult DeleteDivisionById(Guid divisionId)
    {
        Division? division = divisionService.GetDivisionById(divisionId);

        if (division is null)
        {
            return BadRequest($"Division with id {divisionId} not found.");
        }

        divisionService.DeleteDivision(division);
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
    [HttpPut("divisions/{divisionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DivisionResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateDivisionById(Guid divisionId, DivisionRequest divisionRequest)
    {
        Division? existingDivision = divisionService.GetDivisionById(divisionId);

        if (existingDivision is null)
        {
            return BadRequest($"Division with id {divisionId} not found.");
        }

        mapper.Map(divisionRequest, existingDivision);
        bool updateResult = await divisionService.UpdateDivision(existingDivision);

        return !updateResult ? BadRequest("Failed to update the division.") : Ok();
    }
}
