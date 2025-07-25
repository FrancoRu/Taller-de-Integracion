using Entities.DTOs.Abstract;
using Entities.DTOs.Stage;
using Entities.Models.Stages;
namespace Services.Services.Stages;

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

    /// <summary>
    /// Deletes a Stage from the system.
    /// </summary>
    /// <param name="stageEntity">Stage entity to delete.</param>
    /// <returns>True if deletion was successful; false otherwise.</returns>
    Task<bool> DeleteStageAsync(Stage stageEntity);

    /// <summary>
    /// Updates an existing Stage.
    /// </summary>
    /// <param name="stageEntity">Stage entity with updated data.</param>
    /// <returns>The updated Stage entity.</returns>
    Task<Stage> UpdateStageAsync(Stage stageEntity);

    /// <summary>
    /// Creates a new Stage.
    /// </summary>
    /// <param name="stageEntity">Stage entity to create.</param>
    /// <returns>The created Stage entity.</returns>
    Task<Stage> CreateStageAsync(Stage stageEntity);
}

