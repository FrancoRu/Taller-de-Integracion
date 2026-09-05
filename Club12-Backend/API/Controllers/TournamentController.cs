using API.Utils;

using Application.DTOs.Abstract.Response;
using Application.DTOs.Champions.Response;
using Application.DTOs.Divisions.Response;
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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Controllers;

/// <summary>
/// Manages tournaments: create, retrieve, update, delete, filter, and register teams. Reads are public; writes require Owner or Admin.
/// </summary>
[Route("api/tournaments/")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
public class TournamentController(
    ITournamentService tournamentService,
    ITeamService teamService,
    IChampionService championService,
    IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Creates a new tournament.
    /// </summary>
    /// <param name="tournamentRequest">Tournament creation data.</param>
    /// <returns>
    /// Returns 201 Created with the created tournament details.
    /// Returns 400 Bad Request if the request is invalid.
    /// Returns 403 Forbidden if the user is not authorized.
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
    /// Creates a whole tournament, including its divisions' cups, points, playoff mappings, and stages, in one atomic transaction.
    /// </summary>
    /// <param name="request">The full tournament-wizard payload.</param>
    /// <returns>
    /// Returns 201 Created with the created tournament, including its divisions.
    /// Returns 400 Bad Request if the payload is invalid or a rule, for
    /// example a division category mismatch, aborts the atomic create.
    /// Returns 403 Forbidden if the user is not authorized.
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
    /// Adds one division, with its group stage, cups, and playoff mappings, to an existing tournament in one atomic transaction, only while OpenForRegistration.
    /// </summary>
    /// <param name="tournamentId">The parent tournament's id.</param>
    /// <param name="request">The division's structure, zone or cross-cup.</param>
    /// <returns>
    /// Returns 201 Created with the created division.
    /// Returns 404 Not Found if the tournament does not exist.
    /// Returns 409 Conflict if the tournament is not OpenForRegistration or
    /// the division's category does not match the tournament's.
    /// </returns>
    [HttpPost("{tournamentId:guid}/divisions/full")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(DivisionResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DivisionResponse>> AddFullDivision(
        Guid tournamentId, CreateFullDivisionRequest request)
    {
        Tournament? tournament = await tournamentService.GetTournamentByIdAsync(tournamentId);
        if (tournament is null)
        {
            return this.NotFoundProblem(nameof(Tournament), tournamentId);
        }

        Division createdDivision = await tournamentService.AddFullDivisionAsync(tournament, request);
        DivisionResponse divisionResponse = mapper.Map<DivisionResponse>(createdDivision);

        return new ObjectResult(divisionResponse) { StatusCode = StatusCodes.Status201Created };
    }

    /// <summary>
    /// Retrieves a tournament by its unique identifier or its public slug.
    /// </summary>
    /// <param name="idOrSlug">Tournament's GUID identifier or slug.</param>
    /// <returns>
    /// Returns 200 OK with tournament details if found.
    /// Returns 404 Not Found if not found.
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
    /// Retrieves each division's podium: champion, runner-up, and third place, decided by playoff or group standings, with undecided places left null.
    /// </summary>
    /// <param name="idOrSlug">Tournament's GUID identifier or slug.</param>
    /// <returns>
    /// Returns 200 OK with one podium per division.
    /// Returns 404 Not Found if the tournament does not exist.
    /// </returns>
    [AllowAnonymous]
    [HttpGet("{idOrSlug}/champions")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<PodiumResponse>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<PodiumResponse>>> GetTournamentChampions(string idOrSlug)
    {
        Tournament? tournament = await tournamentService.GetTournamentByIdOrSlugAsync(idOrSlug);

        if (tournament is null)
        {
            return this.NotFoundProblem(nameof(Tournament), idOrSlug);
        }

        List<PodiumResponse> podiums = await championService.GetTournamentChampionsAsync(tournament.Id);
        return Ok(podiums);
    }

    /// <summary>
    /// Updates a tournament's descriptive fields; a Status change is routed through a forward-only state machine instead of being written directly.
    /// </summary>
    /// <param name="id">Tournament's GUID identifier.</param>
    /// <param name="tournamentRequest">Tournament update data.</param>
    /// <returns>
    /// Returns 200 OK if updated successfully.
    /// Returns 404 Not Found if the tournament does not exist.
    /// Returns 403 Forbidden if unauthorized.
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

        // Status is intentionally excluded from this mapping since descriptive fields update freely here, but a status change is a guarded state-machine transition handled below.
        mapper.Map(tournamentRequest, existingTournament);

        await tournamentService.UpdateTournamentAsync(existingTournament);

        // Routes any requested status change through the forward-only state machine; a no-op transition where the status is unchanged is ignored by the service, so re-sending the current status on a plain edit is harmless.
        if (tournamentRequest.Status is TournamentStatus requestedStatus)
        {
            await tournamentService.ChangeStatusAsync(id, requestedStatus);
        }

        return NoContent();
    }

    /// <summary>
    /// Deletes a tournament by its identifier.
    /// </summary>
    /// <param name="id">Tournament's GUID identifier.</param>
    /// <returns>
    /// Returns 200 OK if deleted successfully.
    /// Returns 400 Bad Request if not found.
    /// Returns 403 Forbidden if unauthorized.
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
    /// Returns 200 OK with paginated tournament results.
    /// Returns 400 Bad Request if parameters are invalid.
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
    /// Enrolls a team into the tournament's registration phase, either as a new team or an existing club with an optional roster copy, in one transaction.
    /// </summary>
    /// <param name="tournamentId">Tournament's GUID identifier.</param>
    /// <param name="request">The enroll payload.</param>
    /// <returns>
    /// Returns 201 Created with the enrolled team, its roster scoped to this tournament.
    /// Returns 400 Bad Request when the payload shape is invalid: not exactly one of
    /// ExistingTeamId or NewTeamName, or CopyRosterFromTournamentId without ExistingTeamId.
    /// Returns 404 Not Found when the tournament or an existing team does not exist.
    /// Returns 409 Conflict when the tournament is not OpenForRegistration or the team
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

    /// <summary>
    /// Reports whether the tournament can be completed once started and the blocking issues when it cannot.
    /// </summary>
    /// <param name="tournamentId">Tournament's GUID identifier.</param>
    /// <returns>
    /// Returns 200 OK with the completability report, CanStart plus Issues.
    /// Returns 404 Not Found when the tournament does not exist.
    /// Returns 403 Forbidden if unauthorized.
    /// </returns>
    [HttpGet("{tournamentId:guid}/completability")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TournamentCompletabilityResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TournamentCompletabilityResponse>> GetCompletability(Guid tournamentId)
    {
        TournamentCompletabilityResponse response = await tournamentService.GetCompletabilityAsync(tournamentId);
        return Ok(response);
    }

    /// <summary>
    /// Removes a team from the tournament during its registration or pre-start window, clearing only this tournament's footprint for the team.
    /// </summary>
    /// <param name="tournamentId">Tournament's GUID identifier.</param>
    /// <param name="teamId">Team's GUID identifier.</param>
    /// <returns>
    /// Returns 204 No Content when the team is removed.
    /// Returns 404 Not Found when the tournament does not exist or the team is not enrolled.
    /// Returns 409 Conflict when the tournament has already started and is not in a removable phase.
    /// Returns 403 Forbidden if unauthorized.
    /// </returns>
    [HttpDelete("{tournamentId:guid}/teams/{teamId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UnenrollTeam(Guid tournamentId, Guid teamId)
    {
        Tournament? tournament = await tournamentService.GetTournamentByIdAsync(tournamentId);
        if (tournament is null)
        {
            return this.NotFoundProblem(nameof(Tournament), tournamentId);
        }

        await teamService.UnenrollTeamAsync(tournament, teamId);
        return NoContent();
    }
}
