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
/// Controller for managing Stage entities, providing endpoints for creation, retrieval, update, deletion,
/// automated generation of stages and matches, and team assignment operations. Reads are public; writes
/// require Owner or Admin.
/// </summary>
/// <remarks>
/// This controller exposes RESTful endpoints for handling Stage-related operations in the tournament system.
/// It supports creating new stages, generating stages and matches for a division, retrieving stages by ID or with filters,
/// updating and deleting stages, and assigning or unassigning teams to stages.
/// The controller leverages dependency injection for services and AutoMapper for DTO mapping.
/// </remarks>
/// <param name="stageService">Service for Stage business logic and persistence operations.</param>
/// <param name="matchService">Service for Match business logic and automated match generation.</param>
/// <param name="mapper">AutoMapper instance for mapping between entities and DTOs.</param>
[Route("api/stages/")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
public class StageController(IStageService stageService, IMatchService matchService, IMapper mapper) : ControllerBase
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
    /// Generates all stages and matches for the specified division and returns the resulting stage information.
    /// </summary>
    /// <remarks>
    /// Use this endpoint to automatically create the full set of stages and matches for a division.
    /// This operation is typically used after a division has been set up and is ready for scheduling.
    /// </remarks>
    /// <param name="id">The unique identifier of the division for which stages and matches are to be generated.</param>
    /// <returns>An HTTP 200 OK response containing a list of stage details if the operation succeeds.</returns>
    [HttpPost("generate/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<StageResponse>))]
    public async Task<ActionResult> GenerateStagesAndMatches(Guid id)
    {
        List<Stage> response = await stageService.CreateAutomatedStagesAsync(divisionId: id);

        foreach (Stage stage in response)
        {
            stage.Matches = await matchService.CreateAutomatedMatchesAsync(stageId: stage.Id);
        }

        return Ok(mapper.Map<List<StageResponse>>(response));
    }

    /// <summary>
    /// Retrieves a Stage by its id or its public slug.
    /// </summary>
    /// <param name="idOrSlug">The id (GUID) or slug of the Stage.</param>
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
    /// <remarks>
    /// This endpoint allows assigning teams to a stage, either manually or automatically.
    /// The assignment is based on the provided team IDs and the 'Auto' flag in the request.
    /// </remarks>
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
    /// Seeds an elimination stage's already-generated matches from the
    /// division's group-stage standings, using the classic bracket seed
    /// order (1v8, 4v5, 2v7, 3v6) so the top two seeds only meet in the
    /// final.
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
}
