using Application.DTOs.Abstract.Response;
using Application.DTOs.Stage.Request;
using Application.DTOs.Stage.Response;

using Domain.Entities.Models;
using Domain.Enums;

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
    /// Thrown when a stage with the same name already exists in the division.
    /// </exception>
    Task<Stage> CreateStageAsync(Stage stageEntity);

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

    /// <summary>
    /// Computes a first-round pairing for a groupless bracket without persisting it, returning a signed token that replays the exact same order on commit.
    /// </summary>
    /// <param name="stageId">The bracket's first-round stage.</param>
    /// <param name="mode">Whether the order is shuffled server-side or supplied manually.</param>
    /// <param name="manualOrder">The explicit team order, required and validated as a roster permutation when mode is Manual.</param>
    /// <returns>The previewed pairing together with a draw token that commit can replay.</returns>
    Task<DrawPreviewResult> PreviewDrawAsync(Guid stageId, DrawMode mode, List<Guid>? manualOrder = null);

    /// <summary>
    /// Seeds a groupless bracket from a previewed token or a manual order, stamping DrawnAt and auditing the draw.
    /// </summary>
    /// <param name="stageId">The bracket's first-round stage.</param>
    /// <param name="mode">Whether the order comes from a previewed random draw or a manual order.</param>
    /// <param name="drawToken">The token returned by a prior preview, required and verified when mode is Random.</param>
    /// <param name="manualOrder">The explicit team order, required and validated as a roster permutation when mode is Manual.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown as a 409 when a real match of this bracket has already been played, or the draw token is missing, tampered, or mismatched.
    /// </exception>
    /// <returns>The bracket's first-round matches, now seeded with home/visitor teams.</returns>
    Task<List<Match>> CommitDrawAsync(Guid stageId, DrawMode mode, string? drawToken = null, List<Guid>? manualOrder = null);

    /// <summary>
    /// Replaces a regular division's sub-group stage layer with a new count, re-balancing the untouched roster across it.
    /// </summary>
    /// <param name="divisionId">The division whose sub-groups are rebuilt.</param>
    /// <param name="subGroupCount">The new number of sub-groups, at least 1.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown as a 409 when the tournament has already started, the roster is too small for the
    /// requested count, or the division already carries a position-range playoff cup.
    /// </exception>
    /// <returns>The newly created sub-group stages.</returns>
    Task<List<Stage>> RebuildSubGroupsAsync(Guid divisionId, int subGroupCount);

    /// <summary>
    /// Clears every current sub-group placement and re-runs balanced distribution over the division's whole roster.
    /// </summary>
    /// <param name="divisionId">The division whose sub-groups are re-balanced.</param>
    Task AutoDistributeRosterAsync(Guid divisionId);

    /// <summary>
    /// Manually moves one enrolled team from one sub-group to another of the same division, re-validating only the minimum sub-group size.
    /// </summary>
    /// <param name="teamId">The team to move.</param>
    /// <param name="fromStageId">The sub-group the team currently belongs to.</param>
    /// <param name="toStageId">The sub-group the team moves into.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown as a 409 when the tournament has already started, the team is not placed in
    /// fromStageId, the two stages belong to different divisions, or the move would drop the
    /// source sub-group below the minimum size.
    /// </exception>
    Task ReassignTeamToSubGroupAsync(Guid teamId, Guid fromStageId, Guid toStageId);
}

