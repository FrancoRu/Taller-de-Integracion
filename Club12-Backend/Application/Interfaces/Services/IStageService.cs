using Application.DTOs.Abstract.Response;
using Application.DTOs.Stage.Request;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace Application.Interfaces.Services;

public interface IStageService
{
    Task<Stage?> GetStageByIdAsync(Guid stageId);

    /// <summary>
    /// Retrieves a stage by its id or its slug asynchronously, treating the value as an id when it parses as a GUID and otherwise looking it up as a slug.
    /// </summary>
    /// <param name="idOrSlug">The stage's GUID id or its slug.</param>
    /// <returns>The matching stage, or null if not found.</returns>
    Task<Stage?> GetStageByIdOrSlugAsync(string idOrSlug);

    /// <summary>
    /// Retrieves a paginated and filtered list of Stages.
    /// </summary>
    /// <param name="filter">Object containing parameters to filter, sort, and paginate the results.</param>
    /// <returns>A paginated response with the list of Stages.</returns>
    Task<PaginatedResponse<Stage>> GetAllStagesAsync(GetStagesFilteredRequest filter);

    /// <summary>
    /// Deletes a stage, blocked once its tournament has started.
    /// </summary>
    /// <param name="id">The unique identifier of the stage to delete.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the stage's tournament has already started or was canceled.
    /// </exception>
    Task DeleteStageAsync(Guid id);

    /// <summary>
    /// Updates a stage, blocked once its tournament has started.
    /// </summary>
    /// <param name="stageEntity">Stage entity with updated data.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown, mapped to 409, when the tournament has already started or was canceled.
    /// </exception>
    Task UpdateStageAsync(Stage stageEntity);

    /// <summary>
    /// Creates a stage, blocked once its tournament has started.
    /// </summary>
    /// <param name="stageEntity">Stage entity to create.</param>
    /// <returns>The created Stage entity.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a stage with the same name already exists in the division,
    /// or when a non-cross-division-cup division already has a Group stage
    /// and stageEntity is also a Group stage.
    /// </exception>
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

    /// <summary>
    /// Seeds the first-round matches of an elimination stage using the division's group-stage standings and the classic bracket seed order, so the top seeds only meet in the final.
    /// </summary>
    /// <param name="stageId">The elimination stage to seed.</param>
    /// <returns>The stage's matches, now seeded with home/visitor teams.</returns>
    Task<List<Match>> SeedKnockoutStageAsync(Guid stageId);

    /// <summary>
    /// Seeds every playoff cup of a division from its final group-stage standings using the division's position-range mapping.
    /// </summary>
    /// <param name="divisionId">The division whose group stage has finished.</param>
    /// <returns>The seeded matches per destination cup, keyed by BracketName.</returns>
    Task<Dictionary<string, List<Match>>> SeedPlayoffCupsAsync(Guid divisionId);

    /// <summary>
    /// Automatically seeds a division's playoff cups after a match finishes, once every match of every group stage in the division is complete.
    /// </summary>
    /// <param name="finishedMatchStageId">The stage of the match that just finished.</param>
    Task TryAutoSeedPlayoffPhaseAsync(Guid finishedMatchStageId);

    /// <summary>
    /// Pushes each newly-decided bracket slot's winner into its immediate next round within the same cup after a match or series is decided.
    /// </summary>
    /// <param name="decidedStageId">The stage whose slots just got decided.</param>
    Task TryAdvanceStageWinnerAsync(Guid decidedStageId);
}

