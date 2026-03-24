using AutoMapper;
using Application.DTOs.Abstract.Response;
using Application.DTOs.Tournament.Request;
using Application.DTOs.Tournament.Response;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Domain.Entities.Models;

namespace API.Controllers;

/// <summary>
/// Controller responsible for managing tournament-related operations.
/// Provides endpoints for creating, retrieving, updating, deleting, and filtering tournaments,
/// as well as registering teams to tournaments.
/// </summary>
[Route("api/tournaments/")]
[ApiController]
public class TournamentController(
    ITournamentService _tournamentService,
    ITeamService _teamService,
    IMapper _mapper) : ControllerBase
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
        Tournament mappedTournament = _mapper.Map<Tournament>(tournamentRequest);
        Tournament createdTournament = await _tournamentService.CreateTournamentAsync(mappedTournament);
        TournamentResponse tournamentResponse = _mapper.Map<TournamentResponse>(createdTournament);

        return new ObjectResult(tournamentResponse) { StatusCode = StatusCodes.Status201Created };
    }

    /// <summary>
    /// Retrieves a tournament by its unique identifier.
    /// </summary>
    /// <param name="id">Tournament identifier (GUID).</param>
    /// <returns>
    /// Returns 200 (OK) with tournament details if found.
    /// Returns 400 (Bad Request) if not found.
    /// </returns>
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TournamentResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TournamentResponse>> GetTournamentById(Guid id)
    {
        Tournament? tournament = await _tournamentService.GetTournamentByIdAsync(id);

        if (tournament is null)
        {
            return BadRequest($"Tournament with id {id} not found.");
        }

        TournamentResponse tournamentResponse = _mapper.Map<TournamentResponse>(tournament);
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
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdateTournamentAsync(Guid id, CreateTournamentRequest tournamentRequest)
    {
        Tournament? existingTournament = await _tournamentService.GetTournamentByIdAsync(id);

        if (existingTournament is null)
        {
            return BadRequest($"Tournament with id {id} not found.");
        }

        _mapper.Map(tournamentRequest, existingTournament);

        await _tournamentService.UpdateTournamentAsync(existingTournament);

        return Ok();
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
        await _tournamentService.DeleteTournamentAsync(id);
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
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<TournamentResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<TournamentResponse>>> GetFilteredTournaments([FromQuery] GetTournamentsFilteredRequest filterRequest)
    {
        PaginatedResponse<Tournament> paginatedTournaments = await _tournamentService.GetAllTournamentsAsync(filterRequest);
        PaginatedResponse<TournamentResponse> response = _mapper.Map<PaginatedResponse<TournamentResponse>>(paginatedTournaments);

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
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> RegisterTeam(Guid id, RegisterTeamsInTournamentRequest registerTeamsRequest)
    {
        Tournament? tournament = await _tournamentService.GetTournamentByIdAsync(id);
        if (tournament is null)
        {
            return BadRequest($"Tournament with id {id} not found.");
        }
        await _teamService.RegisterTeamsToTournamentAsync(tournament, registerTeamsRequest.TeamIds);
        return Ok();
    }
}
