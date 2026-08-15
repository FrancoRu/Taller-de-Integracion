using Application.DTOs.Abstract.Response;
using Application.DTOs.Divisions.Request;
using Application.DTOs.Divisions.Response;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace API.Controllers;

/// <summary>
/// Controller for managing divisions.
/// </summary>
/// <param name="divisionService">The division service.</param>
/// <param name="mapper">The AutoMapper instance.</param>
//[Authorize(Roles = "SuperAdmin")]
[Route("api/divisions/")]
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
    [HttpPost()]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(DetailedDivisionResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DivisionResponse>> CreateDivision(CreateDivisionRequest divisionRequest)
    {
        Division mappedDivision = mapper.Map<Division>(divisionRequest);
        Division createdDivision = await divisionService.CreateDivisionAsync(mappedDivision);
        DivisionResponse divisionResponse = mapper.Map<DivisionResponse>(createdDivision);

        return CreatedAtAction(nameof(GetDivisionById),new { id = divisionResponse.Id }, divisionResponse);
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
    public async Task<ActionResult<DivisionResponse>> GetDivisionById(Guid id)
    {
        Division? division = await divisionService.GetSimpleDivisionByIdAsync(id);

        if (division is null)
        {
            return BadRequest($"Division with id {id} not found.");
        }

        DivisionResponse divisionResponse = mapper.Map<DivisionResponse>(division);

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
        
        await divisionService.DeleteDivisionAsync(id);
        return NoContent();

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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateDivisionById(Guid id, UpdateDivisionRequest divisionRequest)
    {
        Division? existingDivision = await divisionService.GetFullDivisionByIdAsync(id);

        if (existingDivision is null)
        {
            return BadRequest($"Division with id {id} not found.");
        }

        mapper.Map(divisionRequest, existingDivision);
        await divisionService.UpdateDivisionAsync(existingDivision);

        return Ok();
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
        PaginatedResponse<Division> paginatedDivisions = await divisionService.GetAllDivisionsAsync(filterRequest);

        PaginatedResponse<DivisionResponse> response = mapper.Map<PaginatedResponse<DivisionResponse>>(paginatedDivisions);

        return Ok(response);
    }

}
