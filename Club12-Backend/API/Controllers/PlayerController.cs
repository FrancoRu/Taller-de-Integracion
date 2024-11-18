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
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(PlayerResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PlayerResponse>> CreatePlayerAsync(CreatePlayerRequest playerRequest)
    {
        Guid TeamId = playerRequest.TeamId;
        Team? existingTeam = await _teamService.GetTeamByIdAsync(TeamId);

        if (existingTeam is null)
        {
            return BadRequest($"There is no Team with id: {TeamId}.");
        }

        Player mappedPlayer = _mapper.Map<Player>(playerRequest);
        Player createdPlayer = await _playerService.CreatePlayerAsync(mappedPlayer);
        PlayerResponse playerResponse = _mapper.Map<PlayerResponse>(createdPlayer);

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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PlayerResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlayerResponse>> GetPlayerByIdAsync(Guid id)
    {
        Player? player = await _playerService.GetPlayerByIdAsync(id);

        if (player is null)
        {
            return BadRequest($"Player with id {id} not found.");
        }

        PlayerResponse playerResponse = _mapper.Map<PlayerResponse>(player);
        return Ok(playerResponse);
    }

    /// <summary>
    /// Updates a player by its id.
    /// </summary>
    /// <param name="playerId">The id of the player to update.</param>
    /// <param name="playerRequest">The player request.</param>
    /// <returns>
    /// Returns 200 (OK) with the updated Player response if the update was successful.
    /// Returns 400 (Bad Request) if the Player with the provided id was not found.
    /// Returns 403 (Forbidden) if the user is not authenticated.
    /// </returns>
    [HttpPut("{playerId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdatePlayerAsync(Guid playerId, UpdatePlayerRequest playerRequest)
    {
        Player? existingPlayer = await _playerService.GetPlayerByIdAsync(playerId);

        if (existingPlayer is null)
        {
            return BadRequest($"Player with id {playerId} not found.");
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
    [HttpGet()]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<PlayerResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<PlayerResponse>>> GetFilteredPlayersAsync([FromQuery] GetPlayersFilteredRequest filterRequest)
    {
        PaginatedResponse<Player> paginatedPlayers = await _playerService.GetAllPlayersAsync(filterRequest);

        PaginatedResponse<PlayerResponse> response = _mapper.Map<PaginatedResponse<PlayerResponse>>(paginatedPlayers);

        return Ok(response);
    }
}
