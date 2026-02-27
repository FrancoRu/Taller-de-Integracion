using Application.DTOs.Abstract.Response;
using Application.DTOs.Stage.Request;
using Domain.Entities.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace Application.Interfaces.Services;

/// <summary>
/// Service for managing Stage entities.
/// </summary>
public interface IStageService
{
    /// <summary>
    /// Retrieves a Stage by its identifier.
    /// </summary>
    /// <param name="stageId">Unique identifier of the Stage.</param>
    /// <returns>The found Stage or null if it does not exist.</returns>
    Task<Stage?> GetStageByIdAsync(Guid stageId);

    /// <summary>
    /// Retrieves a paginated and filtered list of Stages.
    /// </summary>
    /// <param name="filter">Object containing parameters to filter, sort, and paginate the results.</param>
    /// <returns>A paginated response with the list of Stages.</returns>
    Task<PaginatedResponse<Stage>> GetAllStagesAsync(GetStagesFilteredRequest filter);

    Task DeleteStageAsync(Guid id);

    /// <summary>
    /// Updates an existing Stage.
    /// </summary>
    /// <param name="stageEntity">Stage entity with updated data.</param>
    /// <returns>The updated Stage entity.</returns>
    Task UpdateStageAsync(Stage stageEntity);

    /// <summary>
    /// Creates a new Stage.
    /// </summary>
    /// <param name="stageEntity">Stage entity to create.</param>
    /// <returns>The created Stage entity.</returns>
    Task<Stage> CreateStageAsync(Stage stageEntity);

    /// <summary>
    /// Asynchronously creates a list of automated stages for the specified division.
    /// </summary>
    /// <param name="divisionId">The unique identifier of the division for which to create automated stages.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of automated stages created
    /// for the specified division. The list will be empty if no stages are created.</returns>
    Task<List<Stage>> CreateAutomatedStagesAsync(Guid divisionId);

    Task AssignTeamsToStageAsync(Stage stage, List<Guid>? teamIds = null, bool auto = false);
    Task UnassignTeamsFromStageAsync(Stage stage, List<Guid> teamIds);
}

