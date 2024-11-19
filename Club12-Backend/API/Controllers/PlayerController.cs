using AutoMapper;

using Entities.DTOs.Abstract;
using Entities.DTOs.Player;
using Entities.Models.PlayerEntity;
using Entities.Models.TeamEntity;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Services.Services.PlayerService;
using Services.Services.TeamService;

namespace Club12.API.Controllers;

/// <summary>
/// Controller for managing Players.
/// </summary>
/// <param name="_playerService">The Player service.</param>
/// <param name="_teamService">The Team service.</param>
/// <param name="_mapper">The AutoMapper instance.</param>
[Authorize(Roles = "SuperAdmin")]
[Route("api/players/")]
[ApiController]
public class PlayerController(
    IPlayerService _playerService,
    ITeamService _teamService,
    IMapper _mapper
    ) : ControllerBase
{
    /// <summary>
    /// Creates a new player.
    /// </summary>
    /// <param name="playerRequest">The player request.</param>
    /// <returns>The created Player response.
    /// <para>Returns 201 (Created) with the Player response if the creation was successful.</para>
    /// <para>Returns 400 (Bad Request) if the Team with the provided id was not found.</para>
    /// <para>Returns 403 (Forbidden) if the user is not authenticated.</para>
    /// </returns>
    [HttpPost()]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(PublicPlayerResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PublicPlayerResponse>> CreatePlayerAsync(CreatePlayerRequest playerRequest)
    {
        Guid TeamId = playerRequest.TeamId;
        Team? existingTeam = await _teamService.GetTeamByIdAsync(TeamId);

        if (existingTeam is null)
        {
            return BadRequest($"There is no Team with id: {TeamId}.");
        }

        Player mappedPlayer = _mapper.Map<Player>(playerRequest);
        Player createdPlayer = await _playerService.CreatePlayerAsync(mappedPlayer);
        PublicPlayerResponse playerResponse = _mapper.Map<PublicPlayerResponse>(createdPlayer);

        return new ObjectResult(playerResponse) { StatusCode = StatusCodes.Status201Created };
    }

    /// <summary>
    /// Retrieves a player by its id.
    /// </summary>
    /// <param name="id">The id of the player to retrieve.</param>
    /// <returns>The Player with the specified id.
    /// <para>Returns 200 (OK) with the Player response if it was found.</para>
    /// <para>Returns 400 (Bad Request) if the Player with the provided id was not found.</para>
    /// </returns>
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PublicPlayerResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PublicPlayerResponse>> GetPlayerByIdAsync(Guid id)
    {
        Player? player = await _playerService.GetPlayerByIdAsync(id);

        if (player is null)
        {
            return BadRequest($"Player with id {id} not found.");
        }

        PublicPlayerResponse playerResponse = _mapper.Map<PublicPlayerResponse>(player);
        return Ok(playerResponse);
    }

    /// <summary>
    /// Updates a player by its id.
    /// </summary>
    /// <param name="id">The id of the player to update.</param>
    /// <param name="playerRequest">The player request.</param>
    /// <returns>
    /// Returns 200 (OK) with the updated Player response if the update was successful.
    /// Returns 400 (Bad Request) if the Player with the provided id was not found.
    /// Returns 403 (Forbidden) if the user is not authenticated.
    /// </returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdatePlayerAsync(Guid id, UpdatePlayerRequest playerRequest)
    {
        Player? existingPlayer = await _playerService.GetPlayerByIdAsync(id);

        if (existingPlayer is null)
        {
            return BadRequest($"Player with id {id} not found.");
        }

        _mapper.Map(playerRequest, existingPlayer);
        bool updateResult = await _playerService.UpdatePlayerAsync(existingPlayer);

        return !updateResult ? BadRequest("Failed to update the player.") : NoContent();
    }

    /// <summary>
    /// Deletes a player by its id.
    /// </summary>
    /// <param name="id">The id of the Player to delete.</param>
    /// <returns>
    /// Returns 200 (OK) if the Player was successfully deleted.
    /// Returns 400 (Bad Request) if the Player with the provided id was not found.
    /// Returns 403 (Forbidden) if the user is not authenticated.
    /// </returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeletePlayerByIdAsync(Guid id)
    {
        Player? player = await _playerService.GetPlayerByIdAsync(id);

        if (player is null)
        {
            return BadRequest($"Player with id {id} not found.");
        }

        await _playerService.DeletePlayerAsync(player);
        return NoContent();
    }

    /// <summary>
    /// Retrieves filtered players with pagination.
    /// </summary>
    /// <param name="filterRequest">The filtering and pagination parameters.</param>
    /// <returns>A paginated response containing the filtered players.</returns>
    [AllowAnonymous]
    [HttpGet("public")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<PublicPlayerResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<PublicPlayerResponse>>> GetFilteredPlayersAsync([FromQuery] GetPublicPlayersFilteredRequest filterRequest)
    {
        PaginatedResponse<Player> paginatedPlayers = await _playerService.GetAllPlayersAsync(filterRequest);

        PaginatedResponse<PublicPlayerResponse> response = _mapper.Map<PaginatedResponse<PublicPlayerResponse>>(paginatedPlayers);

        return Ok(response);
    }

    /// <summary>
    /// Retrieves filtered players with pagination and detailed information for admins.
    /// This endpoint is for private use only and requires admin access.
    /// </summary>
    /// <param name="filterRequest"> The filtering and pagination parameters. This includes optional query parameters like:    /// </param>
    /// <returns> A paginated response containing the filtered players. </returns>
    /// <response code="200">Returns a paginated list of filtered players</response>
    /// <response code="400">Returns 400 if there is an invalid filter parameter or the filter results in no data</response>
    /// <response code="403">Returns 403 if the user does not have the required permissions (admin)</response>
    [HttpGet("")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<AdminPlayerResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResponse<AdminPlayerResponse>>> GetFilteredPlayersPrivateAsync([FromQuery] GetPlayersFilteredRequest filterRequest)
    {
        PaginatedResponse<Player> paginatedPlayers = await _playerService.GetAllPlayersAsync(filterRequest);

        PaginatedResponse<AdminPlayerResponse> response = _mapper.Map<PaginatedResponse<AdminPlayerResponse>>(paginatedPlayers);

        return Ok(response);
    }
}
