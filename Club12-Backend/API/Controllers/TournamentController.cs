using AutoMapper;

using Entities.DTOs.Abstract;
using Entities.DTOs.Tournament;
using Entities.Models.Tournaments;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Services.Services.Tournaments;

namespace Club12.API.Controllers;

/// <summary>
/// Controller for managing Tournaments.
/// </summary>
/// <param name="_tournamentService">The Tournament service.</param>
/// <param name="_mapper">The AutoMapper instance.</param>
//[Authorize(Roles = "SuperAdmin")]
[Route("api/tournaments/")]
[ApiController]
public class TournamentController(
    ITournamentService _tournamentService,
    IMapper _mapper) : ControllerBase
{

    /// <summary>
    /// Creates a new tournament.
    /// </summary>
    /// <param name="tournamentRequest">The tournament request.</param>
    /// <returns>The created Tournament response.
    /// <para>Returns 201 (Created) with the Tournament response if the creation was successful.</para>
    /// <para>Returns 400 (Bad Request) if there was an error in the request.</para>
    /// <para>Returns 403 (Forbidden) if the user is not authenticated.</para>
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
    /// Retrieves a tournament by its Id.
    /// </summary>
    /// <param name="id">The id of the tournament to retrieve.</param>
    /// <returns>The Tournament with the specified Id.
    /// <para>Returns 200 (OK) with the Tournament response if it was found.</para>
    /// <para>Returns 400 (Bad Request) if the Tournament with the provided id was not found.</para>
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
    /// Updates a tournament by its id.
    /// </summary>
    /// <param name="id">The id of the tournament to update.</param>
    /// <param name="tournamentRequest">The tournament request.</param>
    /// <returns>
    /// Returns 200 (OK) with the updated Tournament response if the update was successful.
    /// Returns 400 (Bad Request) if the Tournament with the provided id was not found.
    /// Returns 403 (Forbidden) if the user is not authenticated.
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

        _mapper.Map(tournamentRequest, existingTournament);;

        return Ok(await _tournamentService.UpdateTournamentAsync(existingTournament));
    }

    /// <summary>
    /// Deletes a tournament by its id.
    /// </summary>
    /// <param name="id">The id of the Tournament to delete.</param>
    /// <returns>
    /// Returns 200 (OK) if the Tournament was successfully deleted.
    /// Returns 400 (Bad Request) if the Tournament with the provided id was not found.
    /// Returns 403 (Forbidden) if the user is not authenticated.
    /// </returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteTournamentById(Guid id)
    {
        Tournament? tournament = await _tournamentService.GetTournamentByIdAsync(id);

        if (tournament is null)
        {
            return BadRequest($"Tournament with id {id} not found.");
        }

        bool deleteResult = await _tournamentService.DeleteTournamentAsync(tournament);
        return deleteResult ? Ok() : BadRequest("Failed to delete the tournament.");
    }

    /// <summary>
    /// Retrieves filtered tournaments with pagination.
    /// </summary>
    /// <param name="filterRequest">The filtering and pagination parameters.</param>
    /// <returns>A paginated response containing the filtered tournaments.</returns>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<TournamentResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<TournamentResponse>>> GetFilteredTournaments([FromQuery] GetTournamentsFilteredRequest filterRequest)
    {
        PaginatedResponse<Tournament> paginatedTournaments = await _tournamentService.GetAllTournamentsAsync(filterRequest);
        PaginatedResponse<TournamentResponse> response = _mapper.Map<PaginatedResponse<TournamentResponse>>(paginatedTournaments);

        return Ok(response);
    }
}
