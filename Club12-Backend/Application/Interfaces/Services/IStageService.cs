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
    /// Retrieves a stage by its id or its slug asynchronously. The value is
    /// treated as an id when it parses as a GUID, otherwise it is looked up as
    /// a slug.
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

    /// <summary>
    /// Seeds the first-round matches of an elimination stage using the
    /// division's group-stage standings and the classic bracket seed order
    /// (1 vs 8, 4 vs 5, 2 vs 7, 3 vs 6 for 8 teams), so the top seeds only
    /// meet in the final. Requires the stage's matches to already exist
    /// (via CreateAutomatedMatchesAsync) and be unseeded, and every team
    /// assigned to the stage to already have a finished-group-stage
    /// position — a power-of-two number of ranked teams is required.
    /// </summary>
    /// <param name="stageId">The elimination stage to seed.</param>
    /// <returns>The stage's matches, now seeded with home/visitor teams.</returns>
    Task<List<Match>> SeedKnockoutStageAsync(Guid stageId);

    /// <summary>
    /// Seeds every playoff cup of a division from its final group-stage
    /// standings using the division's position-range mapping (HU-45/HU-81).
    /// Each mapped destination is populated from the standings positions its
    /// range covers and seeded into the first-round elimination stage whose
    /// BracketName matches that destination.
    /// </summary>
    /// <param name="divisionId">The division whose group stage has finished.</param>
    /// <returns>The seeded matches per destination cup (BracketName → matches).</returns>
    Task<Dictionary<string, List<Match>>> SeedPlayoffCupsAsync(Guid divisionId);

    /// <summary>
    /// Called after a match finishes: if <paramref name="finishedMatchStageId"/>
    /// belongs to a Group stage, and that finish completed EVERY match of
    /// EVERY group stage in the division (the whole group phase), and the
    /// division uses position-range playoff cups (HU-45/HU-81) that are not
    /// already (even partially) seeded, automatically seeds them via
    /// <see cref="SeedPlayoffCupsAsync"/> — the admin no longer has to click
    /// "Sembrar bracket" by hand once every zone game is in. A no-op for
    /// every other case (mid-group-phase, no cups configured, or already
    /// seeded — left for the admin's manual action). Never throws: an
    /// auto-seed failure is logged and swallowed rather than failing the
    /// match-finishing operation that triggered it.
    /// </summary>
    /// <param name="finishedMatchStageId">The stage of the match that just finished.</param>
    Task TryAutoSeedPlayoffPhaseAsync(Guid finishedMatchStageId);

    /// <summary>
    /// Called after a match (or a best-of-N series it belongs to) is decided:
    /// pushes each newly-decided bracket slot's winner into its immediate next
    /// round within the same cup (same Division + BracketName), matching the
    /// slot up using the classic bracket adjacency (slot 2i and 2i+1 of this
    /// stage feed slot i of the next stage, as Home and Visitor respectively —
    /// the same convention <see cref="SeedPlayoffCupsAsync"/> establishes for
    /// the first round). If the next stage's own <c>BestOf</c> is greater than
    /// 1 and BOTH its Home and Visitor are now known, converts that slot into
    /// game 1 of a new <c>MatchSeries</c> — the same treatment a freshly-seeded
    /// series gets. A no-op for the Final (nothing further to advance to), for
    /// a Group stage, or when nothing new was decided. Never throws: a failure
    /// is logged and swallowed rather than failing the result-loading
    /// operation that triggered it.
    /// </summary>
    /// <param name="decidedStageId">The stage whose slot(s) just got decided.</param>
    Task TryAdvanceStageWinnerAsync(Guid decidedStageId);
}

