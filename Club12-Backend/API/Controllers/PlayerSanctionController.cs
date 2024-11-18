using AutoMapper;

using Entities.DTOs.PlayerSanction;
using Entities.Models.PlayerSanctionEntity;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Services.Services.PlayerSanctionService;

namespace Club12.API.Controllers;

/// <summary>
/// Controller for managing Player Sanctions.
/// </summary>
/// <param name="_playerSanctionService">The Player Sanction service.</param>
/// <param name="_mapper">The AutoMapper instance.</param>
[Authorize(Roles = "SuperAdmin")]
[Route("api/player-sanctions/")]
[ApiController]
public class PlayerSanctionController(IPlayerSanctionService _playerSanctionService, IMapper _mapper) : ControllerBase
{
    /// <summary>
    /// Creates a new player sanction.
    /// </summary>
    /// <param name="playerSanctionRequest">The player sanction request DTO.</param>
    /// <returns>The created player sanction response.</returns>
    [HttpPost()]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(PlayerSanctionResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PlayerSanctionResponse>> CreatePlayerSanction(CreatePlayerSanctionRequest playerSanctionRequest)
    {
        PlayerSanction mappedSanction = _mapper.Map<PlayerSanction>(playerSanctionRequest);
        PlayerSanction createdSanction = await _playerSanctionService.CreatePlayerSanctionAsync(mappedSanction);
        PlayerSanctionResponse sanctionResponse = _mapper.Map<PlayerSanctionResponse>(createdSanction);

        return new ObjectResult(sanctionResponse) { StatusCode = StatusCodes.Status201Created };
    }

    /// <summary>
    /// Retrieves a player sanction by its id.
    /// </summary>
    /// <param name="id">The id of the player sanction.</param>
    /// <returns>The player sanction response DTO.</returns>
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PlayerSanctionResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlayerSanctionResponse>> GetPlayerSanctionById(Guid id)
    {
        PlayerSanction? sanction = await _playerSanctionService.GetPlayerSanctionByIdAsync(id);

        if (sanction is null)
        {
            return BadRequest($"Player sanction with id {id} not found.");
        }

        PlayerSanctionResponse sanctionResponse = _mapper.Map<PlayerSanctionResponse>(sanction);
        return Ok(sanctionResponse);
    }

    /// <summary>
    /// Updates a player sanction asynchronously.
    /// </summary>
    /// <param name="sanctionId">The id of the sanction to update.</param>
    /// <param name="updateRequest">The request with updated sanction data.</param>
    /// <returns>Returns the result of the update operation.</returns>
    [HttpPut("{sanctionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdatePlayerSanction(Guid sanctionId, UpdatePlayerSanctionRequest updateRequest)
    {
        PlayerSanction? existingSanction = await _playerSanctionService.GetPlayerSanctionByIdAsync(sanctionId);

        if (existingSanction is null)
        {
            return BadRequest($"Player sanction with id {sanctionId} not found.");
        }

        _mapper.Map(updateRequest, existingSanction);
        bool updateResult = await _playerSanctionService.UpdatePlayerSanctionAsync(existingSanction);

        return !updateResult ? BadRequest("Failed to update the player sanction.") : NoContent();
    }

    /// <summary>
    /// Deletes a player sanction by its id.
    /// </summary>
    /// <param name="id">The id of the player sanction to delete.</param>
    /// <returns>Returns the result of the delete operation.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> DeletePlayerSanctionById(Guid id)
    {
        PlayerSanction? sanction = await _playerSanctionService.GetPlayerSanctionByIdAsync(id);

        if (sanction is null)
        {
            return BadRequest($"Player sanction with id {id} not found.");
        }

        bool deleteResult = await _playerSanctionService.DeletePlayerSanctionAsync(sanction);
        return deleteResult ? BadRequest() : NoContent();
    }
}
