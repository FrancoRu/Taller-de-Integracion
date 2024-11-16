using AutoMapper;

using Entities.DTOs.Abstract;
using Entities.DTOs.Tournament;
using Entities.Models.TournamentEntity;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Services.Services.TournamentService;

namespace Club12.API.Controllers;

/// <summary>
/// Controller for managing Tournaments.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TournamentController"/> class.
/// </remarks>
/// <param name="_tournamentService">The Tournament service.</param>
/// <param name="_mapper">The AutoMapper instance.</param>
[Authorize(Roles = "SuperAdmin")]
[Route("api/tournaments/")]
[ApiController]
public class TournamentController(
    ITournamentService _tournamentService,
    IMapper _mapper
    ) : ControllerBase
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
    [HttpPost()]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TournamentResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<TournamentResponse> CreateTournament(CreateTournamentRequest tournamentRequest)
    {
        Tournament mappedTournament = _mapper.Map<Tournament>(tournamentRequest);
        Tournament createdTournament = _tournamentService.CreateTournament(mappedTournament);
        TournamentResponse tournamentResponse = _mapper.Map<TournamentResponse>(createdTournament);

        return new ObjectResult(tournamentResponse) { StatusCode = StatusCodes.Status201Created };
    }

    /// <summary>
    /// Retrieves a tournament by its Id.
    /// </summary>
    /// <param name="tournamentId">The id of the tournament to retrieve.</param>
    /// <returns>The Tournament with the specified Id.
    /// <para>Returns 200 (OK) with the Tournament response if it was found.</para>
    /// <para>Returns 400 (Bad Request) if the Tournament with the provided tournamentId was not found.</para>
    /// </returns>
    [AllowAnonymous]
    [HttpGet("{tournamentId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TournamentResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<TournamentResponse> GetTournamentById(Guid tournamentId)
    {
        Tournament? tournament = _tournamentService.GetTournamentById(tournamentId);

        if (tournament is null)
        {
            return BadRequest($"Tournament with id {tournamentId} not found.");
        }

        TournamentResponse tournamentResponse = _mapper.Map<TournamentResponse>(tournament);
        return Ok(tournamentResponse);
    }

    /// <summary>
    /// Updates a tournament by its tournamentId.
    /// </summary>
    /// <param name="tournamentId">The tournamentId of the tournament to update.</param>
    /// <param name="tournamentRequest">The tournament request.</param>
    /// <returns>
    /// Returns 200 (OK) with the updated Tournament response if the update was successful.
    /// Returns 400 (Bad Request) if the Tournament with the provided tournamentId was not found.
    /// Returns 403 (Forbidden) if the user is not authenticated.
    /// </returns>
    [HttpPut("{tournamentId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TournamentResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdateTournament(Guid tournamentId, CreateTournamentRequest tournamentRequest)
    {
        Tournament? existingTournament = _tournamentService.GetTournamentById(tournamentId);

        if (existingTournament is null)
        {
            return BadRequest($"Tournament with id {tournamentId} not found.");
        }

        _mapper.Map(tournamentRequest, existingTournament);
        bool updateResult = await _tournamentService.UpdateTournamentAsync(existingTournament);

        return !updateResult ? BadRequest("Failed to update the tournament.") : Ok();
    }

    /// <summary>
    /// Deletes a tournament by its tournamentId.
    /// </summary>
    /// <param name="tournamentId">The tournamentId of the Tournament to delete.</param>
    /// <returns>
    /// Returns 200 (OK) if the Tournament was successfully deleted.
    /// Returns 400 (Bad Request) if the Tournament with the provided tournamentId was not found.
    /// Returns 403 (Forbidden) if the user is not authenticated.
    /// </returns>
    [HttpDelete("{tournamentId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult DeleteTournamentById(Guid tournamentId)
    {
        Tournament? tournament = _tournamentService.GetTournamentById(tournamentId);

        if (tournament is null)
        {
            return BadRequest($"Tournament with id {tournamentId} not found.");
        }

        _tournamentService.DeleteTournament(tournament);
        return Ok();
    }

    /// <summary>
    /// Retrieves filtered tournaments with pagination.
    /// </summary>
    /// <param name="filterRequest">The filtering and pagination parameters.</param>
    /// <returns>A paginated response containing the filtered tournaments.</returns>
    [AllowAnonymous]
    [HttpGet()]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<TournamentResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<TournamentResponse>>> GetFilteredTournaments([FromQuery] GetTournamentsFilteredRequest filterRequest)
    {
        PaginatedResponse<Tournament> paginatedTournaments = await _tournamentService.GetAllTournamentsAsync(filterRequest);

        PaginatedResponse<TournamentResponse> response = _mapper.Map<PaginatedResponse<TournamentResponse>>(paginatedTournaments);

        return Ok(response);
    }
}
