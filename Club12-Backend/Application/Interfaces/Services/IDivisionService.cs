using Application.DTOs.Abstract.Response;
using Application.DTOs.Divisions.Request;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Represents a service for managing divisions.
/// </summary>
public interface IDivisionService
{
    /// <summary>
    /// Creates a new division asynchronously.
    /// </summary>
    /// <param name="divisionEntity">The division entity to create.</param>
    /// <returns>The created division.</returns>
    Task<Division> CreateDivisionAsync(Division divisionEntity);

    /// <summary>
    /// Retrieves a division by its id asynchronously.
    /// </summary>
    /// <param name="divisionId">The id of the division to retrieve.</param>
    /// <returns>The division with the specified id, or null if not found.</returns>
    Task<Division?> GetFullDivisionByIdAsync(Guid divisionId);

    Task<Division?> GetSimpleDivisionByIdAsync(Guid divisionId);

    Task DeleteDivisionAsync(Guid id);

    /// <summary>
    /// Updates a division asynchronously.
    /// </summary>
    /// <param name="divisionEntity">The division to update.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
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
