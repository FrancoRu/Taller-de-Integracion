using Application.DTOs.Abstract.Response;
using Application.DTOs.Divisions.Request;
using Application.Utils.Helper.Standings;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

public interface IDivisionService
{
    /// <summary>
    /// Creates a division and generates its unique slug from the name.
    /// </summary>
    /// <param name="divisionEntity">The division entity to create.</param>
    /// <returns>The created division.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the tournament is not <see cref="Domain.Enums.TournamentStatus.OpenForRegistration"/>
    /// (HU-31, structure is frozen once registration closes), when the
    /// division's <see cref="Division.Category"/> does not match its
    /// tournament's category (HU-48), or when its playoff mappings are invalid.
    /// </exception>
    Task<Division> CreateDivisionAsync(Division divisionEntity);

    Task<Division?> GetFullDivisionByIdAsync(Guid divisionId);

    Task<Division?> GetSimpleDivisionByIdAsync(Guid divisionId);

    /// <summary>
    /// Retrieves a division by its id or its slug asynchronously. The value is
    /// treated as an id when it parses as a GUID, otherwise it is looked up as
    /// a slug.
    /// </summary>
    /// <param name="idOrSlug">The division's GUID id or its slug.</param>
    /// <returns>The matching division, or null if not found.</returns>
    Task<Division?> GetSimpleDivisionByIdOrSlugAsync(string idOrSlug);

    /// <summary>
    /// Deletes a division. A division owns its stages, matches, statistics and
    /// point deductions, all of which cascade at the database level, so this
    /// is blocked whenever that history exists or would be destroyed silently.
    /// </summary>
    /// <param name="id">The unique identifier of the division to delete.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the division has any finished match or point deduction, or
    /// its tournament has already started (Ongoing/Finished) or was Canceled.
    /// </exception>
    Task DeleteDivisionAsync(Guid id);

    /// <summary>
    /// Updates a division, re-validating it against the same tournament-status
    /// and category rules enforced on create (see <see cref="CreateDivisionAsync"/>).
    /// </summary>
    /// <param name="divisionEntity">The division entity with updated values.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the tournament no longer allows structural edits, the
    /// category no longer matches, or the playoff mappings are invalid.
    /// </exception>
    Task UpdateDivisionAsync(Division divisionEntity);

    /// <summary>
    /// Retrieves divisions with pagination and filtering asynchronously.
    /// </summary>
    /// <param name="filter">The filtering and pagination request.</param>
    /// <returns>A paginated response containing the divisions.</returns>
    Task<PaginatedResponse<Division>> GetAllDivisionsAsync(GetDivisionsFilteredRequest filter);

    /// <summary>
    /// Computes standings for a division from its Group stage's finished
    /// matches. Elimination-stage matches do not feed a standings table.
    /// </summary>
    /// <param name="divisionId">The id of the division.</param>
    /// <returns>One Position per team with at least one finished Group-stage match; empty if the division has no Group stage or no finished matches yet.</returns>
    Task<List<Position>> GetPositionsByDivisionIdAsync(Guid divisionId);

    /// <summary>
    /// Computes standings for a division split by Group stage. A regular zone
    /// (one Group stage) returns a single entry; a multi-group cross-division
    /// cup (HU-110) returns one entry per internal group ("Grupo 1".."Grupo N"),
    /// each computed only over that group's finished matches. Groups are
    /// ordered by stage Order then name. Empty when the division has no Group
    /// stage.
    /// </summary>
    /// <param name="divisionId">The id of the division.</param>
    /// <returns>One <see cref="GroupStandings"/> per Group stage.</returns>
    Task<List<GroupStandings>> GetGroupStandingsByDivisionIdAsync(Guid divisionId);

    /// <summary>
    /// Returns every team registered to the tournament that does not yet
    /// belong to any division (regular or cross-division-cup). A readiness
    /// signal for the tournament-builder UI, not a hard database
    /// constraint — a team is expected to be unassigned while the admin is
    /// still building the tournament out.
    /// </summary>
    /// <param name="tournamentId">The id of the tournament.</param>
    Task<List<Team>> GetUnassignedTeamsAsync(Guid tournamentId);

    /// <summary>
    /// Reassigns a division to a different tournament, moving everything
    /// under it — stages, matches, and team assignments — along with it,
    /// since none of that data carries its own tournament reference. Only
    /// the target tournament's existence is validated here; the mutated
    /// entity is not persisted by this method, so the caller must still
    /// call <see cref="UpdateDivisionAsync"/> to save the change.
    /// </summary>
    /// <param name="division">The division to reassign. Its Tournament navigation and TournamentId are mutated in place.</param>
    /// <param name="tournamentId">The id of the tournament the division should belong to.</param>
    /// <returns>True if the target tournament exists and the division was reassigned in memory; false if no tournament with that id exists.</returns>
    Task<bool> TryAssignTournamentAsync(Division division, Guid tournamentId);
}
