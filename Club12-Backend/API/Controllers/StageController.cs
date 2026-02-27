using AutoMapper;
using Application.DTOs.Abstract.Response;
using Application.DTOs.Stage.Request;
using Application.DTOs.Stage.Response;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.Models;

namespace API.Controllers;

/// <summary>
/// Controller for managing Stage entities.
/// </summary>
/// <param name="_stageService">The Match service.</param>
/// <param name="_mapper">The AutoMapper instance.</param>
//[Authorize(Roles = "SuperAdmin")]
[Route("api/stages/")]
[ApiController]
public class StageController(IStageService _stageService, IMatchService matchService, IMapper _mapper) : ControllerBase
{

    /// <summary>
    /// Creates a new Stage.
    /// </summary>
    /// <param name="stageRequest">The Stage creation request DTO.</param>
    /// <returns>The created Stage response.</returns>
    [HttpPost()]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(StageResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StageResponse>> CreateStage(CreateStageRequest stageRequest)
    {
        Stage mappedStage = _mapper.Map<Stage>(stageRequest);
        Stage createdStage = await _stageService.CreateStageAsync(mappedStage);
        StageResponse stageResponse = _mapper.Map<StageResponse>(createdStage);
        return CreatedAtAction(nameof(GetStageById), new { createdStage.Id }, stageResponse);
    }

    /// <summary>
    /// Generates all stages and matches for the specified division and returns the resulting stage information.
    /// </summary>
    /// <remarks>Use this endpoint to automatically create the full set of stages and matches for a division.
    /// This operation is typically used after a division has been set up and is ready for scheduling.</remarks>
    /// <param name="id">The unique identifier of the division for which stages and matches are to be generated.</param>
    /// <returns>An HTTP 200 OK response containing a list of stage details if the operation succeeds.</returns>
    [HttpPost("generate/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<StageResponse>))]
    public async Task<ActionResult> GenerateStagesAndMatches(Guid id)
    {
        List<Stage> response = await _stageService.CreateAutomatedStagesAsync(divisionId: id);

        foreach (Stage stage in response)
        {
            stage.Matches = await matchService.CreateAutomatedMatchesAsync(stageId: stage.Id);
        }

        return Ok(_mapper.Map<List<StageResponse>>(response));
    }

    /// <summary>
    /// Retrieves a Stage by its id.
    /// </summary>
    /// <param name="id">The id of the Stage.</param>
    /// <returns>The Stage entity.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Stage))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StageResponse>> GetStageById(Guid id)
    {
        Stage? stage = await _stageService.GetStageByIdAsync(id);

        if (stage == null)
            return NotFound($"Stage with id {id} not found.");

        StageResponse stageResponse = _mapper.Map<StageResponse>(stage);
        return Ok(stageResponse);
    }

    /// <summary>
    /// Retrieves filtered and paginated Stages.
    /// </summary>
    /// <param name="filterRequest">The filter and pagination parameters.</param>
    /// <returns>Paginated list of Stages.</returns>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<StageResponse>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResponse<Stage>>> GetFilteredStages([FromQuery] GetStagesFilteredRequest filterRequest)
    {
        PaginatedResponse<Stage> paginatedStages = await _stageService.GetAllStagesAsync(filterRequest);
        PaginatedResponse<StageResponse> paginatedResponse = _mapper.Map<PaginatedResponse<StageResponse>>(paginatedStages);
        return Ok(paginatedResponse);
    }

    /// <summary>
    /// Updates an existing Stage.
    /// </summary>
    /// <param name="id">The id of the Stage to update.</param>
    /// <param name="stageRequest">The updated Stage data.</param>
    /// <returns>The updated Stage entity.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Stage))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Stage>> UpdateStage(Guid id, UpdateStageRequest stageRequest)
    {
        Stage? existingStage = await _stageService.GetStageByIdAsync(id);
        if (existingStage == null)
            return NotFound($"Stage with id {id} not found.");

        _mapper.Map(stageRequest, existingStage);
        
        await _stageService.UpdateStageAsync(existingStage);

        return Ok();
    }

    /// <summary>
    /// Deletes a Stage by its id.
    /// </summary>
    /// <param name="id">The id of the Stage to delete.</param>
    /// <returns>Result of the delete operation.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteStage(Guid id)
    {
       
        await _stageService.DeleteStageAsync(id);

        return NoContent();
    }

    [HttpPost("{id:guid}/assign-team")]
    public async Task<ActionResult> AssignamentTeam(Guid id, AssignamentTeamRequest request)
    {
        Stage? stage = await _stageService.GetStageByIdAsync(id);

        if (stage == null)
            return NotFound($"Stage with id {id} not found.");

        await _stageService.AssignTeamsToStageAsync(stage, request.TeamIds, request.Auto);

        return Ok();
    }

    [HttpDelete("{id:guid}/unassign-team")]
    public async Task<ActionResult> UnassignamentTeam(Guid id, UnassignamentTeamRequest request)
    {
        Stage? stage = await _stageService.GetStageByIdAsync(id);

        if (stage == null)
            return NotFound($"Stage with id {id} not found.");
        await _stageService.UnassignTeamsFromStageAsync(stage, request.TeamIds);
        return Ok();
    }
}
