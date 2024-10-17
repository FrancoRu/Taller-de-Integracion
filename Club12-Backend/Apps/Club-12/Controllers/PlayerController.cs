using AutoMapper;
using Club12.Entities.PlayerEntity;
using Club12.Entities.TeamEntity;
using Club12.Services.DTOs.Player;
using Club12.Services.Services.PlayerService;
using Club12.Services.Services.TeamService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Club12.Controllers;

/// <summary>
/// Controller for managing Players.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PlayerController"/> class.
/// </remarks>
/// <param name="playerService">The Player service.</param>
/// <param name="teamService">The Team service.</param>
/// <param name="mapper">The AutoMapper instance.</param>
[Authorize(Roles = "SuperAdmin")]
[Route("api/")]
[ApiController]
public class PlayerController(
    IPlayerService playerService,
    ITeamService teamService,
    IMapper mapper
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
    [HttpPost("players")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(PlayerResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<PlayerResponse> CreatePlayer(CreatePlayerRequest playerRequest)
    {
        Guid TeamId = playerRequest.TeamId;
        Team? existingTeam = teamService.GetTeamById(TeamId);

        if (existingTeam is null)
        {
            return BadRequest($"There is no Team with id: {TeamId}.");
        }

        Player mappedPlayer = mapper.Map<Player>(playerRequest);
        Player createdPlayer = playerService.CreatePlayer(mappedPlayer);
        PlayerResponse playerResponse = mapper.Map<PlayerResponse>(createdPlayer);

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
    [HttpGet("players/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PlayerResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<PlayerResponse> GetPlayerById(Guid id)
    {
        Player? player = playerService.GetPlayerById(id);

        if (player is null)
        {
            return BadRequest($"Player with id {id} not found.");
        }

        PlayerResponse playerResponse = mapper.Map<PlayerResponse>(player);
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
    [HttpPut("players/{playerId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PlayerResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdatePlayer(Guid playerId, CreatePlayerRequest playerRequest)
    {
        Player? existingPlayer = playerService.GetPlayerById(playerId);

        if (existingPlayer is null)
        {
            return BadRequest($"Player with id {playerId} not found.");
        }

        mapper.Map(playerRequest, existingPlayer);
        bool updateResult = await playerService.UpdatePlayer(existingPlayer);

        return !updateResult ? BadRequest("Failed to update the player.") : Ok();
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
    [HttpDelete("players/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult DeletePlayerById(Guid id)
    {
        Player? player = playerService.GetPlayerById(id);

        if (player is null)
        {
            return BadRequest($"Player with id {id} not found.");
        }

        playerService.DeletePlayer(player);
        return Ok();
    }
}
