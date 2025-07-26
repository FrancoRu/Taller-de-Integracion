using AutoMapper;
using Entities.DTOs.Abstract;
using Entities.DTOs.Stage;
using Entities.Models.Stages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Services.Stages;

namespace Club12.API.Controllers;

/// <summary>
/// Controller for managing Stage entities.
/// </summary>
/// <param name="_stageService">The Match service.</param>
/// <param name="_mapper">The AutoMapper instance.</param>
//[Authorize(Roles = "SuperAdmin")]
[Route("api/stages/")]
[ApiController]
public class StageController(IStageService _stageService, IMapper _mapper) : ControllerBase
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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<Stage>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResponse<Stage>>> GetFilteredMatches([FromQuery] GetStagesFilteredRequest filterRequest)
    {
        PaginatedResponse<Stage> pagedStages = await _stageService.GetAllStagesAsync(filterRequest);
        return Ok(pagedStages);
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
        Stage result = await _stageService.UpdateStageAsync(existingStage);
        return Ok(result);
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
        Stage? stage = await _stageService.GetStageByIdAsync(id);

        if (stage == null)
            return NotFound($"Stage with id {id} not found.");

        bool deleted = await _stageService.DeleteStageAsync(stage);

        return deleted ? NoContent() : BadRequest("Failed to delete the Stage.");
    }
}
