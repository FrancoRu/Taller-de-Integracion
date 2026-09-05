using API.Utils;

using Application.DTOs.PointDeductions.Request;
using Application.DTOs.PointDeductions.Response;
using Application.Interfaces.Services;

using AutoMapper;

using Domain.Entities.Models;
using Domain.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Controllers;

/// <summary>
/// Manages disciplinary point deductions applied to teams within a division; creating and deleting require Admin or Owner while listing is public.
/// </summary>
/// <param name="deductionService">The point-deduction service.</param>
/// <param name="divisionService">The division service, used to validate the division.</param>
/// <param name="teamService">The team service, used to validate the team.</param>
/// <param name="mapper">The AutoMapper instance.</param>
[Route("api/")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
public class PointDeductionController(
    ITeamPointDeductionService deductionService,
    IDivisionService divisionService,
    ITeamService teamService,
    IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Applies a point deduction to a team in a division.
    /// </summary>
    /// <param name="divisionId">The division whose standings the penalty affects.</param>
    /// <param name="request">The deduction, including team, points, and reason.</param>
    /// <returns>
    /// 201 Created with the deduction; 404 Not Found when the division or
    /// team does not exist; 403 Forbidden for non-staff callers.
    /// </returns>
    [HttpPost("divisions/{divisionId:guid}/point-deductions")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(PointDeductionResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PointDeductionResponse>> CreatePointDeduction(
        Guid divisionId, CreatePointDeductionRequest request)
    {
        Division? division = await divisionService.GetSimpleDivisionByIdAsync(divisionId);
        if (division is null)
        {
            return this.NotFoundProblem(nameof(Division), divisionId);
        }

        Team? team = await teamService.GetTeamByIdAsync(request.TeamId);
        if (team is null)
        {
            return this.NotFoundProblem(nameof(Team), request.TeamId);
        }

        TeamPointDeduction deduction = mapper.Map<TeamPointDeduction>(request);
        deduction.DivisionId = divisionId;

        TeamPointDeduction created = await deductionService.CreateAsync(deduction);
        PointDeductionResponse response = mapper.Map<PointDeductionResponse>(created);

        return new ObjectResult(response) { StatusCode = StatusCodes.Status201Created };
    }

    /// <summary>
    /// Lists every point deduction applied in a division, public so standings can show each penalty.
    /// </summary>
    /// <param name="divisionId">The division whose deductions to list.</param>
    /// <returns>200 OK with the deductions, newest first.</returns>
    [AllowAnonymous]
    [HttpGet("divisions/{divisionId:guid}/point-deductions")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<PointDeductionResponse>))]
    public async Task<ActionResult<List<PointDeductionResponse>>> GetPointDeductions(Guid divisionId)
    {
        List<TeamPointDeduction> deductions = await deductionService.GetByDivisionIdAsync(divisionId);
        return Ok(mapper.Map<List<PointDeductionResponse>>(deductions));
    }

    /// <summary>
    /// Removes a point deduction by its id.
    /// </summary>
    /// <param name="id">The id of the deduction to remove.</param>
    /// <returns>204 No Content; 403 Forbidden for non-staff callers.</returns>
    [HttpDelete("point-deductions/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeletePointDeduction(Guid id)
    {
        await deductionService.DeleteAsync(id);
        return NoContent();
    }
}
