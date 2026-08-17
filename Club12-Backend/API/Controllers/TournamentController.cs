using API.Utils;

using Application.DTOs.Abstract.Response;
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
/// TournamentManager.
/// </summary>
[Route("api/tournaments/")]
[ApiController]
[Authorize(Roles = Roles.AdminOwnerOrTournamentManager)]
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

        mapper.Map(tournamentRequest, existingTournament);

        await tournamentService.UpdateTournamentAsync(existingTournament);

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
}
