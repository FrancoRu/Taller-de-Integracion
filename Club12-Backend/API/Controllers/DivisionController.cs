using API.Utils;

using Application.DTOs.Abstract.Response;
using Application.DTOs.Divisions.Request;
using Application.DTOs.Divisions.Response;
using Application.Interfaces.Services;
using Application.Utils.Helper.Standings;

using AutoMapper;

using Domain.Entities.Models;
using Domain.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers;

/// <summary>
/// Manages divisions; reads are public while writes require Owner or Admin.
/// </summary>
/// <param name="divisionService">The division service.</param>
/// <param name="mapper">The AutoMapper instance.</param>
[Route("api/divisions/")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
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
    /// <para>Returns 201 Created with the division response if the creation was successful.</para>
    /// <para>Returns 403 Forbidden if the user is not authenticated.</para>
    /// </returns>
    [HttpPost()]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(DetailedDivisionResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DivisionResponse>> CreateDivision(CreateDivisionRequest divisionRequest)
    {
        Division mappedDivision = mapper.Map<Division>(divisionRequest);
        Division createdDivision = await divisionService.CreateDivisionAsync(mappedDivision);
        DivisionResponse divisionResponse = mapper.Map<DivisionResponse>(createdDivision);

        return CreatedAtAction(nameof(GetDivisionById), new { idOrSlug = divisionResponse.Id }, divisionResponse);
    }

    /// <summary>
    /// Retrieves a division by its id or its public slug.
    /// </summary>
    /// <param name="idOrSlug">The GUID id or slug of the division to retrieve.</param>
    /// <returns>The division with the specified id or slug.
    /// <para>Returns 200 Ok with the division response if it was found.</para>
    /// <para>Returns 404 Not Found if the division with the provided id or slug was not found.</para>
    /// </returns>
    [AllowAnonymous]
    [HttpGet("{idOrSlug}/detail")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DetailedDivisionResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DivisionResponse>> GetDivisionById(string idOrSlug)
    {
        Division? division = await divisionService.GetSimpleDivisionByIdOrSlugAsync(idOrSlug);

        if (division is null)
        {
            return this.NotFoundProblem(nameof(Division), idOrSlug);
        }

        DivisionResponse divisionResponse = mapper.Map<DivisionResponse>(division);
        await PopulateStandingsAsync(divisionResponse);

        return Ok(divisionResponse);
    }

    /// <summary>
    /// Deletes a division by its id.
    /// </summary>
    /// <param name="id">The id of the division to delete.</param>
    /// <returns>
    /// Returns 200 Ok if the division was successfully deleted.
    /// Returns 400 Bad Request if the division with the provided id was not found.
    /// Returns 403 Forbidden if the user is not authenticated.
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
    /// <param name="divisionRequest">
    /// The updated division information. If TournamentId is set, the
    /// division, and everything under it, is moved to that tournament.
    /// </param>
    /// <returns>
    /// Returns 200 Ok with the updated division response if the update was successful.
    /// Returns 404 Not Found if the division, or the target tournament when reassigning, was not found.
    /// Returns 403 Forbidden if the user is not authenticated.
    /// </returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DivisionResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateDivisionById(Guid id, UpdateDivisionRequest divisionRequest)
    {
        Division? existingDivision = await divisionService.GetFullDivisionByIdAsync(id);

        if (existingDivision is null)
        {
            return this.NotFoundProblem(nameof(Division), id);
        }

        mapper.Map(divisionRequest, existingDivision);

        if (divisionRequest.TournamentId is Guid tournamentId)
        {
            bool tournamentAssigned = await divisionService.TryAssignTournamentAsync(existingDivision, tournamentId);
            if (!tournamentAssigned)
            {
                return this.NotFoundProblem(nameof(Tournament), tournamentId);
            }
        }

        await divisionService.UpdateDivisionAsync(existingDivision);

        DivisionResponse divisionResponse = mapper.Map<DivisionResponse>(existingDivision);
        return Ok(divisionResponse);
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

        // Each division's standings must be populated here too, since the divisions table's team counter reads Positions.Length, and leaving it null made every row show 0 teams even when the division was fully populated.
        foreach (DivisionResponse divisionResponse in response.Items)
        {
            await PopulateStandingsAsync(divisionResponse);
        }

        return Ok(response);
    }

    /// <summary>
    /// Fills a division response's standings from its Group stages, setting GroupStandings to one table per group and Positions to the pooled union across all groups so the team counter reflects every group's teams.
    /// </summary>
    private async Task PopulateStandingsAsync(DivisionResponse divisionResponse)
    {
        List<GroupStandings> groups = await divisionService.GetGroupStandingsByDivisionIdAsync(divisionResponse.Id);

        divisionResponse.GroupStandings = mapper.Map<List<GroupStandingsResponse>>(groups);

        List<Position> pooledPositions = [.. groups
            .SelectMany(group => group.Positions)
            .GroupBy(position => position.TeamId)
            .Select(group => group.First())];

        divisionResponse.Positions = mapper.Map<List<PositionResponse>>(pooledPositions);
    }

}
