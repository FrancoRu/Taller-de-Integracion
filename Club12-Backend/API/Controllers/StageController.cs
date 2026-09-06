using Application.DTOs.Abstract.Response;
using Application.DTOs.Match.Response;
using Application.DTOs.Stage.Request;
using Application.DTOs.Stage.Response;
using Application.Interfaces.Services;
using Application.Utils.Constants;

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
/// Manages Stage entities: creation, retrieval, update, deletion, and team assignment. Reads are public; writes require Owner or Admin.
/// </summary>
/// <param name="stageService">Service for Stage business logic and persistence operations.</param>
/// <param name="mapper">AutoMapper instance for mapping between entities and DTOs.</param>
[Route("api/stages/")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
public class StageController(IStageService stageService, IMapper mapper) : ControllerBase
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
        Stage mappedStage = mapper.Map<Stage>(stageRequest);
        Stage createdStage = await stageService.CreateStageAsync(mappedStage);
        StageResponse stageResponse = mapper.Map<StageResponse>(createdStage);
        return CreatedAtAction(nameof(GetStageById), new { idOrSlug = createdStage.Id }, stageResponse);
    }

    /// <summary>
    /// Retrieves a Stage by its id or its public slug.
    /// </summary>
    /// <param name="idOrSlug">The Stage's GUID id or slug.</param>
    /// <returns>The Stage entity.</returns>
    [AllowAnonymous]
    [HttpGet("{idOrSlug}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Stage))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StageResponse>> GetStageById(string idOrSlug)
    {
        Stage? stage = await stageService.GetStageByIdOrSlugAsync(idOrSlug);

        if (stage == null)
        {
            return NotFound(ErrorMessages.Stage.NotFoundById(idOrSlug));
        }

        StageResponse stageResponse = mapper.Map<StageResponse>(stage);
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
        PaginatedResponse<Stage> paginatedStages = await stageService.GetAllStagesAsync(filterRequest);
        PaginatedResponse<StageResponse> paginatedResponse = mapper.Map<PaginatedResponse<StageResponse>>(paginatedStages);
        return Ok(paginatedResponse);
    }

    /// <summary>
    /// Updates an existing Stage.
    /// </summary>
    /// <param name="id">The id of the Stage to update.</param>
    /// <param name="stageRequest">The updated Stage data.</param>
    /// <returns>The updated Stage entity.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StageResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StageResponse>> UpdateStage(Guid id, UpdateStageRequest stageRequest)
    {
        Stage? existingStage = await stageService.GetStageByIdAsync(id);
        if (existingStage == null)
        {
            return NotFound(ErrorMessages.Stage.NotFoundById(id));
        }

        mapper.Map(stageRequest, existingStage);
        await stageService.UpdateStageAsync(existingStage);

        StageResponse stageResponse = mapper.Map<StageResponse>(existingStage);
        return Ok(stageResponse);
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
        await stageService.DeleteStageAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Assigns one or more teams to a specific stage.
    /// </summary>
    /// <param name="id">The unique identifier of the stage to which teams will be assigned.</param>
    /// <param name="request">The assignment request containing team IDs and assignment mode.</param>
    /// <returns>
    /// Returns HTTP 200 OK if the assignment is successful, or 404 Not Found if the stage does not exist.
    /// </returns>
    [HttpPost("{id:guid}/assign-team")]
    public async Task<ActionResult> AssignmentTeam(Guid id, AssignmentTeamRequest request)
    {
        Stage? stage = await stageService.GetStageByIdAsync(id);

        if (stage == null)
        {
            return NotFound(ErrorMessages.Stage.NotFoundById(id));
        }

        await stageService.AssignTeamsToStageAsync(stage, request.TeamIds, request.Auto);

        return Ok();
    }

    /// <summary>
    /// Unassigns one or more teams from a specific stage.
    /// </summary>
    /// <param name="id">The unique identifier of the stage from which teams will be unassigned.</param>
    /// <param name="request">The unassignment request containing team IDs.</param>
    /// <returns>
    /// Returns HTTP 200 OK if the unassignment is successful, or 404 Not Found if the stage does not exist.
    /// </returns>
    [HttpDelete("{id:guid}/unassign-team")]
    public async Task<ActionResult> UnassignmentTeam(Guid id, UnassignmentTeamRequest request)
    {
        Stage? stage = await stageService.GetStageByIdAsync(id);

        if (stage == null)
        {
            return NotFound(ErrorMessages.Stage.NotFoundById(id));
        }

        await stageService.UnassignTeamsFromStageAsync(stage, request.TeamIds);
        return Ok();
    }

    /// <summary>
    /// Seeds an elimination stage's matches from the group-stage standings using the classic 1v8, 4v5, 2v7, 3v6 bracket seed order.
    /// </summary>
    /// <param name="id">The elimination stage to seed.</param>
    /// <returns>
    /// Returns HTTP 200 OK with the now-seeded matches, or 404 Not Found
    /// if the stage does not exist.
    /// </returns>
    [HttpPost("{id:guid}/seed")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<DetailedMatchResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SeedKnockoutStage(Guid id)
    {
        Stage? stage = await stageService.GetStageByIdAsync(id);

        if (stage == null)
        {
            return NotFound(ErrorMessages.Stage.NotFoundById(id));
        }

        List<Match> seededMatches = await stageService.SeedKnockoutStageAsync(id);

        return Ok(mapper.Map<List<DetailedMatchResponse>>(seededMatches));
    }

    /// <summary>
    /// Computes a first-round pairing for a groupless bracket without persisting it, returning a signed token that a later commit can replay exactly.
    /// </summary>
    [HttpPost("{id:guid}/preview-draw")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DrawPreviewResult))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DrawPreviewResult>> PreviewDraw(Guid id, DrawRequest request)
    {
        Stage? stage = await stageService.GetStageByIdAsync(id);

        if (stage == null)
        {
            return NotFound(ErrorMessages.Stage.NotFoundById(id));
        }

        DrawPreviewResult preview = await stageService.PreviewDrawAsync(id, request.Mode, request.ManualOrder);
        return Ok(preview);
    }

    /// <summary>
    /// Seeds a groupless bracket from a previewed token or a manual order, stamping DrawnAt and auditing the draw.
    /// </summary>
    [HttpPost("{id:guid}/draw")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<DetailedMatchResponse>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<List<DetailedMatchResponse>>> CommitDraw(Guid id, DrawRequest request)
    {
        Stage? stage = await stageService.GetStageByIdAsync(id);

        if (stage == null)
        {
            return NotFound(ErrorMessages.Stage.NotFoundById(id));
        }

        List<Match> seededMatches = await stageService.CommitDrawAsync(id, request.Mode, request.DrawToken, request.ManualOrder);
        return Ok(mapper.Map<List<DetailedMatchResponse>>(seededMatches));
    }
}
