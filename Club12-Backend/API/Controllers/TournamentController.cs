using API.Utils;

using Application.DTOs.Abstract.Response;
using Application.DTOs.Team.Response;
using Application.DTOs.Tournament.Request;
using Application.DTOs.Tournament.Response;
using Application.Interfaces.Services;

using AutoMapper;

using Domain.Entities.Models;
using Domain.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Threading.Tasks;

namespace API.Controllers;

/// <summary>
/// Controller responsible for managing tournament-related operations.
/// Provides endpoints for creating, retrieving, updating, deleting, and filtering tournaments,
/// as well as registering teams to tournaments. Reads are public; writes require Owner or
/// Admin.
/// </summary>
[Route("api/tournaments/")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
public class TournamentController(
    ITournamentService tournamentService,
    ITeamService teamService,
    IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Creates a new tournament.
    /// </summary>
    /// <param name="tournamentRequest">Tournament creation data.</param>
    /// <returns>
    /// Returns 201 (Created) with the created tournament details.
    /// Returns 400 (Bad Request) if the request is invalid.
    /// Returns 403 (Forbidden) if the user is not authorized.
    /// </returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TournamentResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TournamentResponse>> CreateTournamentAsync(CreateTournamentRequest tournamentRequest)
    {
        Tournament mappedTournament = mapper.Map<Tournament>(tournamentRequest);
        Tournament createdTournament = await tournamentService.CreateTournamentAsync(mappedTournament);
        TournamentResponse tournamentResponse = mapper.Map<TournamentResponse>(createdTournament);

        return new ObjectResult(tournamentResponse) { StatusCode = StatusCodes.Status201Created };
    }

    /// <summary>
    /// HU-38: creates a whole tournament (base fields + divisions with their
    /// cups, points, playoff mappings and stages) in a single atomic
    /// transaction. A failure at any step persists nothing — no partial
    /// tournament is left behind. The granular endpoints stay available for
    /// incremental edits.
    /// </summary>
    /// <param name="request">The full tournament-wizard payload.</param>
    /// <returns>
    /// Returns 201 (Created) with the created tournament (including its divisions).
    /// Returns 400 (Bad Request) if the payload is invalid or a rule (e.g. a
    /// division category mismatch) aborts the atomic create.
    /// Returns 403 (Forbidden) if the user is not authorized.
    /// </returns>
    [HttpPost("full")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TournamentResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TournamentResponse>> CreateFullTournamentAsync(CreateFullTournamentRequest request)
    {
        Tournament createdTournament = await tournamentService.CreateFullTournamentAsync(request);
        TournamentResponse tournamentResponse = mapper.Map<TournamentResponse>(createdTournament);

        return new ObjectResult(tournamentResponse) { StatusCode = StatusCodes.Status201Created };
    }

    /// <summary>
    /// Retrieves a tournament by its unique identifier or its public slug.
    /// </summary>
    /// <param name="idOrSlug">Tournament identifier (GUID) or slug.</param>
    /// <returns>
    /// Returns 200 (OK) with tournament details if found.
    /// Returns 404 (Not Found) if not found.
    /// </returns>
    [AllowAnonymous]
    [HttpGet("{idOrSlug}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TournamentResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TournamentResponse>> GetTournamentById(string idOrSlug)
    {
        Tournament? tournament = await tournamentService.GetTournamentByIdOrSlugAsync(idOrSlug);

        if (tournament is null)
        {
            return this.NotFoundProblem(nameof(Tournament), idOrSlug);
        }

        TournamentResponse tournamentResponse = mapper.Map<TournamentResponse>(tournament);
        return Ok(tournamentResponse);
    }

    /// <summary>
    /// Updates an existing tournament by its identifier.
    /// </summary>
    /// <param name="id">Tournament identifier (GUID).</param>
    /// <param name="tournamentRequest">Tournament update data.</param>
    /// <returns>
    /// Returns 200 (OK) if updated successfully.
    /// Returns 400 (Bad Request) if not found.
    /// Returns 403 (Forbidden) if unauthorized.
    /// </returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TournamentResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdateTournamentAsync(Guid id, UpdateTournamentRequest tournamentRequest)
    {
        Tournament? existingTournament = await tournamentService.GetTournamentByIdAsync(id);

        if (existingTournament is null)
        {
            return this.NotFoundProblem(nameof(Tournament), id);
        }

        // Status is intentionally excluded from this mapping (see
        // TournamentProfile): descriptive fields update freely here, but a
        // status change is a guarded state-machine transition handled below.
        mapper.Map(tournamentRequest, existingTournament);

        await tournamentService.UpdateTournamentAsync(existingTournament);

        // Route any requested status change through the forward-only state
        // machine. A no-op transition (same status) is ignored by the service,
        // so re-sending the current status on a plain edit is harmless.
        if (tournamentRequest.Status is TournamentStatus requestedStatus)
        {
            await tournamentService.ChangeStatusAsync(id, requestedStatus);
        }

        return NoContent();
    }

    /// <summary>
    /// Deletes a tournament by its identifier.
    /// </summary>
    /// <param name="id">Tournament identifier (GUID).</param>
    /// <returns>
    /// Returns 200 (OK) if deleted successfully.
    /// Returns 400 (Bad Request) if not found.
    /// Returns 403 (Forbidden) if unauthorized.
    /// </returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteTournamentById(Guid id)
    {
        await tournamentService.DeleteTournamentAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Retrieves tournaments filtered and paginated according to provided parameters.
    /// </summary>
    /// <param name="filterRequest">Filtering and pagination parameters.</param>
    /// <returns>
    /// Returns 200 (OK) with paginated tournament results.
    /// Returns 400 (Bad Request) if parameters are invalid.
    /// </returns>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<TournamentResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<TournamentResponse>>> GetFilteredTournaments([FromQuery] GetTournamentsFilteredRequest filterRequest)
    {
        PaginatedResponse<Tournament> paginatedTournaments = await tournamentService.GetAllTournamentsAsync(filterRequest);
        PaginatedResponse<TournamentResponse> response = mapper.Map<PaginatedResponse<TournamentResponse>>(paginatedTournaments);

        return Ok(response);
    }

    /// <summary>
    /// Registers teams to a tournament.
    /// </summary>
    /// <param name="id">Tournament identifier (GUID).</param>
    /// <param name="registerTeamsRequest">Request containing team IDs to register.</param>
    /// <returns>
    /// Returns 200 (OK) if teams registered successfully.
    /// Returns 400 (Bad Request) if tournament not found.
    /// Returns 403 (Forbidden) if unauthorized.
    /// </returns>
    [HttpPost("register-teams/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> RegisterTeam(Guid id, RegisterTeamsInTournamentRequest registerTeamsRequest)
    {
        Tournament? tournament = await tournamentService.GetTournamentByIdAsync(id);
        if (tournament is null)
        {
            return this.NotFoundProblem(nameof(Tournament), id);
        }
        await teamService.RegisterTeamsToTournamentAsync(tournament, registerTeamsRequest.TeamIds);
        return Ok();
    }

    /// <summary>
    /// HU-107: enrolls a single team into the tournament's registration phase.
    /// Two modes — create a brand-new team (<c>NewTeamName</c>) or enroll an
    /// existing club from another season (<c>ExistingTeamId</c>), optionally
    /// copying that team's roster from a past season as an editable base
    /// (<c>CopyRosterFromTournamentId</c>). Everything runs in one transaction.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier (GUID).</param>
    /// <param name="request">The enroll payload.</param>
    /// <returns>
    /// Returns 201 (Created) with the enrolled team, its roster scoped to this tournament.
    /// Returns 400 (Bad Request) when the payload shape is invalid (not exactly one of
    /// ExistingTeamId/NewTeamName, or CopyRosterFromTournamentId without ExistingTeamId).
    /// Returns 404 (Not Found) when the tournament or an existing team does not exist.
    /// Returns 409 (Conflict) when the tournament is not OpenForRegistration or the team
    /// is already enrolled.
    /// </returns>
    [HttpPost("{tournamentId:guid}/enroll-team")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TeamResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TeamResponse>> EnrollTeam(Guid tournamentId, EnrollTeamRequest request)
    {
        bool hasExistingTeam = request.ExistingTeamId.HasValue;
        bool hasNewTeamName = !string.IsNullOrWhiteSpace(request.NewTeamName);

        // Exactly one of ExistingTeamId / NewTeamName must be provided.
        if (hasExistingTeam == hasNewTeamName)
        {
            return BadRequest("Provide exactly one of ExistingTeamId or NewTeamName.");
        }

        // CopyRosterFromTournamentId is only valid together with ExistingTeamId.
        if (request.CopyRosterFromTournamentId.HasValue && !hasExistingTeam)
        {
            return BadRequest("CopyRosterFromTournamentId is only allowed together with ExistingTeamId.");
        }

        Tournament? tournament = await tournamentService.GetTournamentByIdAsync(tournamentId);
        if (tournament is null)
        {
            return this.NotFoundProblem(nameof(Tournament), tournamentId);
        }

        Team enrolledTeam = await teamService.EnrollTeamAsync(
            tournament,
            request.ExistingTeamId,
            request.NewTeamName,
            request.CopyRosterFromTournamentId);

        TeamResponse teamResponse = mapper.Map<TeamResponse>(enrolledTeam);
        return new ObjectResult(teamResponse) { StatusCode = StatusCodes.Status201Created };
    }
}
