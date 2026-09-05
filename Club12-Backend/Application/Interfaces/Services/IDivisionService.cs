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
    /// Thrown when the tournament is not TournamentStatus.OpenForRegistration, since structure is frozen once registration closes, when the division's Division.Category does not match its tournament's category, or when its playoff mappings are invalid.
    /// </exception>
    Task<Division> CreateDivisionAsync(Division divisionEntity);

    Task<Division?> GetFullDivisionByIdAsync(Guid divisionId);

    Task<Division?> GetSimpleDivisionByIdAsync(Guid divisionId);

    /// <summary>
    /// Retrieves a division by its id or its slug asynchronously, treating the value as an id when it parses as a GUID and otherwise looking it up as a slug.
    /// </summary>
    /// <param name="idOrSlug">The division's GUID id or its slug.</param>
    /// <returns>The matching division, or null if not found.</returns>
    Task<Division?> GetSimpleDivisionByIdOrSlugAsync(string idOrSlug);

    /// <summary>
    /// Deletes a division, blocked whenever its match, statistics, or point-deduction history exists or would be destroyed silently.
    /// </summary>
    /// <param name="id">The unique identifier of the division to delete.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the division has any finished match or point deduction, or its tournament has already started, Ongoing or Finished, or was Canceled.
    /// </exception>
    Task DeleteDivisionAsync(Guid id);

    /// <summary>
    /// Updates a division, re-validating it against the same tournament-status and category rules enforced on create. See CreateDivisionAsync.
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
    /// Computes standings for a division from its Group stage's finished matches; elimination-stage matches do not feed a standings table.
    /// </summary>
    /// <param name="divisionId">The id of the division.</param>
    /// <returns>One Position per team with at least one finished Group-stage match; empty if the division has no Group stage or no finished matches yet.</returns>
    Task<List<Position>> GetPositionsByDivisionIdAsync(Guid divisionId);

    /// <summary>
    /// Computes standings for a division split by Group stage, with a regular zone returning a single entry and a multi-group cross-division cup returning one entry per internal group.
    /// </summary>
    /// <param name="divisionId">The id of the division.</param>
    /// <returns>One GroupStandings per Group stage.</returns>
    Task<List<GroupStandings>> GetGroupStandingsByDivisionIdAsync(Guid divisionId);

    /// <summary>
    /// Returns every team registered to the tournament that does not yet belong to any division, regular or cross-division cup.
    /// </summary>
    /// <param name="tournamentId">The id of the tournament.</param>
    Task<List<Team>> GetUnassignedTeamsAsync(Guid tournamentId);

    /// <summary>
    /// Reassigns a division to a different tournament, moving everything under it along with it.
    /// </summary>
    /// <param name="division">The division to reassign. Its Tournament navigation and TournamentId are mutated in place.</param>
    /// <param name="tournamentId">The id of the tournament the division should belong to.</param>
    /// <returns>True if the target tournament exists and the division was reassigned in memory; false if no tournament with that id exists.</returns>
    Task<bool> TryAssignTournamentAsync(Division division, Guid tournamentId);
}
