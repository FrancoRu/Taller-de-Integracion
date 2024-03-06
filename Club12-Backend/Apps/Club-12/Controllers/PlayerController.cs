using AutoMapper;
using Club12.Entities.PlayerEntity;
using Club12.Entities.TeamEntity;
using Club12.Services.Auth;
using Club12.Services.Players;
using Club12.Services.Teams;
using Club12.Viewmodels.Player;
using Microsoft.AspNetCore.Mvc;

namespace Club12.Controllers;

/// <summary>
/// Controller for managing Players.
/// </summary>
[Route("api/")]
[ApiController]
public class PlayerController : ControllerBase
{
    private readonly IPlayerService _playerService;
    private readonly ITeamService _teamService;
    private readonly IMapper _mapper;
    private readonly IAuthService _authService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerController"/> class.
    /// </summary>
    /// <param name="playerService">The Player service.</param>
    /// <param name="teamService">The Team service.</param>
    /// <param name="authService">The authorization service.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    public PlayerController(
        IPlayerService playerService,
        ITeamService teamService,
        IAuthService authService,
        IMapper mapper
    )
    {
        _playerService = playerService;
        _teamService = teamService;
        _authService = authService;
        _mapper = mapper;
    }

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
    public ActionResult<PlayerResponse> CreatePlayer(PlayerRequest playerRequest)
    {
        if (!_authService.IsTokenValid())
        {
            return Forbid("Invalid Token.");
        }
        if (!_authService.IsUserAuthorized())
        {
            return Forbid();
        }

        Guid TeamId = playerRequest.TeamId;
        Team? existingTeam = _teamService.GetTeamById(TeamId);

        if (existingTeam is null)
        {
            return BadRequest($"There is no Team with id: {TeamId}.");
        }

        Player mappedPlayer = _mapper.Map<Player>(playerRequest);
        Player createdPlayer = _playerService.CreatePlayer(mappedPlayer);
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
    [HttpGet("players/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PlayerResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<PlayerResponse> GetPlayerById(Guid id)
    {
        Player? player = _playerService.GetPlayerById(id);

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
    [HttpPut("players/{playerId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PlayerResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdatePlayer(Guid playerId, PlayerRequest playerRequest)
    {
        if (!_authService.IsTokenValid())
        {
            return Forbid("Invalid Token.");
        }
        if (!_authService.IsUserAuthorized())
        {
            return Forbid();
        }

        Player? existingPlayer = _playerService.GetPlayerById(playerId);

        if (existingPlayer is null)
        {
            return BadRequest($"Player with id {playerId} not found.");
        }

        _mapper.Map(playerRequest, existingPlayer);
        bool updateResult = await _playerService.UpdatePlayer(existingPlayer);

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
        if (!_authService.IsTokenValid())
        {
            return Forbid("Invalid Token.");
        }
        if (!_authService.IsUserAuthorized())
        {
            return Forbid();
        }

        Player? player = _playerService.GetPlayerById(id);

        if (player is null)
        {
            return BadRequest($"Player with id {id} not found.");
        }

        _playerService.DeletePlayer(player);
        return Ok();
    }
}
