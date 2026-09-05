using Application.DTOs.Abstract.Response;
using Application.DTOs.Tournament.Request;
using Application.DTOs.Tournament.Response;

using Domain.Entities.Models;
using Domain.Enums;

using System;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

public interface ITournamentService
{
    /// <summary>
    /// Creates a tournament and generates its unique slug from the name. Bare
    /// structural creation only — use <see cref="CreateFullTournamentAsync"/>
    /// to also create its divisions/stages in one transaction.
    /// </summary>
    /// <param name="tournamentEntity">The Tournament entity to create.</param>
    /// <returns>The created Tournament.</returns>
    Task<Tournament> CreateTournamentAsync(Tournament tournamentEntity);

    /// <summary>
    /// HU-38: creates a whole tournament (base fields + every division with its
    /// points, playoff mappings and stages) in a SINGLE transaction, reusing
    /// the granular create logic. A failure at any point rolls the entire graph
    /// back, so no partial tournament is ever left behind. The tournament is
    /// created <see cref="TournamentStatus.OpenForRegistration"/> so its
    /// structure is valid to build (structural creation is part of creation) and
    /// it is ready to register teams; the fixture is still generated later by
    /// the canonical transition to <see cref="TournamentStatus.Ongoing"/>
    /// (starting the tournament, HU-108).
    /// </summary>
    /// <param name="request">The full wizard payload.</param>
    /// <returns>The created Tournament, including its divisions.</returns>
    Task<Tournament> CreateFullTournamentAsync(CreateFullTournamentRequest request);

    /// <summary>
    /// HU-31/HU-112: adds ONE division (with its group stage, cups and playoff
    /// mappings) to an already-existing tournament, in a single transaction —
    /// the same structure guarantee <see cref="CreateFullTournamentAsync"/>
    /// gives each of its divisions. Unlike the granular division-create
    /// endpoint, this never leaves a bare division with no stages behind. Only
    /// allowed while the tournament is <see cref="TournamentStatus.OpenForRegistration"/>
    /// (enforced by the same guard the granular create uses).
    /// </summary>
    /// <param name="tournament">The already-loaded parent tournament.</param>
    /// <param name="divisionRequest">The division's structure (zone or cross-cup).</param>
    /// <returns>The created Division.</returns>
    Task<Division> AddFullDivisionAsync(Tournament tournament, CreateFullDivisionRequest divisionRequest);

    Task<Tournament?> GetTournamentByIdAsync(Guid tournamentId);

    /// <summary>
    /// Retrieves a Tournament by its id or its slug asynchronously. The value is
    /// treated as an id when it parses as a GUID, otherwise it is looked up as a slug.
    /// </summary>
    /// <param name="idOrSlug">The Tournament's GUID id or its slug.</param>
    /// <returns>The matching Tournament, or null if not found.</returns>
    Task<Tournament?> GetTournamentByIdOrSlugAsync(string idOrSlug);

    /// <summary>
    /// Updates a Tournament. Does NOT change the lifecycle status — use
    /// <see cref="ChangeStatusAsync"/> for that.
    /// </summary>
    /// <param name="tournamentEntity">The Tournament to update.</param>
    Task UpdateTournamentAsync(Tournament tournamentEntity);

    /// <summary>
    /// Moves a tournament to a new lifecycle status, enforcing the forward-only
    /// state machine (see <see cref="Domain.Enums.TournamentStatusTransitions"/>).
    /// A no-op when the tournament is already in the target status. Transitioning
    /// into <see cref="TournamentStatus.Ongoing"/> (starting the tournament)
    /// auto-generates the fixture (matches) for every stage of every division
    /// that does not yet have matches, making it the canonical fixture trigger
    /// (HU-108). Closing registration only freezes the roster; it no longer
    /// generates the fixture.
    /// </summary>
    /// <param name="tournamentId">The id of the tournament to transition.</param>
    /// <param name="newStatus">The target lifecycle status.</param>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException">No tournament exists with the given id.</exception>
    /// <exception cref="System.InvalidOperationException">The requested transition is not allowed by the state machine.</exception>
    Task ChangeStatusAsync(Guid tournamentId, TournamentStatus newStatus);

    /// <summary>
    /// HU-109: reports whether a tournament can be COMPLETED once started, and
    /// lists the blocking issues when it cannot. This is the same guard the
    /// transition to <see cref="TournamentStatus.Ongoing"/> enforces, exposed as
    /// a read-only query so the panel can preview the issues before starting.
    /// </summary>
    /// <param name="tournamentId">The id of the tournament to evaluate.</param>
    /// <returns>The completability report (CanStart + Issues).</returns>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException">No tournament exists with the given id.</exception>
    Task<TournamentCompletabilityResponse> GetCompletabilityAsync(Guid tournamentId);

    /// <summary>
    /// Deletes a tournament. Blocked once it has started (Ongoing/Finished)
    /// or has any finished match, so competitive history is never silently
    /// destroyed. Clears the denormalized current-tournament pointer on any
    /// team still pointing at it before the cascade delete removes this
    /// tournament's registrations, divisions, stages and matches — the teams
    /// themselves are never deleted. A no-op when the id does not match a
    /// tournament.
    /// </summary>
    /// <param name="id">The id of the Tournament to delete.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the tournament has already started or has any finished match.
    /// </exception>
    Task DeleteTournamentAsync(Guid id);

    /// <summary>
    /// Retrieves tournaments with pagination and filtering asynchronously.
    /// </summary>
    /// <param name="filter">The filtering and pagination request.</param>
    /// <returns>A paginated response containing the tournaments.</returns>
    Task<PaginatedResponse<Tournament>> GetAllTournamentsAsync(GetTournamentsFilteredRequest filter);
}
