using AutoMapper;

using Entities.DTOs.PlayerStatistic;
using Entities.Models.PlayerStatisticEntity;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Services.Services.PlayerStatisticService;

namespace Club12.API.Controllers;

/// <summary>
/// Controller for managing Player Statistics.
/// </summary>
/// <param name="_playerStatisticService">The Player Statistic service.</param>
/// <param name="_mapper">The Auto_mapper instance.</param>
[Authorize(Roles = "SuperAdmin")]
[Route("api/player-statistics/")]
[ApiController]
public class PlayerStatisticController(IPlayerStatisticService _playerStatisticService, IMapper _mapper) : ControllerBase
{

    /// <summary>
    /// Creates a new player statistic.
    /// </summary>
    /// <param name="playerStatisticRequest">The player statistic request DTO.</param>
    /// <returns>The created player statistic response.</returns>
    [HttpPost()]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(PlayerStatisticResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PlayerStatisticResponse>> CreatePlayerStatistic(CreatePlayerStatisticRequest playerStatisticRequest)
    {
        PlayerStatistic mappedStatistic = _mapper.Map<PlayerStatistic>(playerStatisticRequest);
        PlayerStatistic createdStatistic = await _playerStatisticService.CreatePlayerStatisticAsync(mappedStatistic);
        PlayerStatisticResponse statisticResponse = _mapper.Map<PlayerStatisticResponse>(createdStatistic);

        return new ObjectResult(statisticResponse) { StatusCode = StatusCodes.Status201Created };
    }

    /// <summary>
    /// Retrieves a player statistic by its id.
    /// </summary>
    /// <param name="id">The id of the player statistic.</param>
    /// <returns>The player statistic response DTO.</returns>
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PlayerStatisticResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlayerStatisticResponse>> GetPlayerStatisticById(Guid id)
    {
        PlayerStatistic? statistic = await _playerStatisticService.GetPlayerStatisticByIdAsync(id);

        if (statistic is null)
        {
            return BadRequest($"Player statistic with id {id} not found.");
        }

        PlayerStatisticResponse statisticResponse = _mapper.Map<PlayerStatisticResponse>(statistic);
        return Ok(statisticResponse);
    }

    /// <summary>
    /// Updates a player statistic asynchronously.
    /// </summary>
    /// <param name="id">The id of the statistic to update.</param>
    /// <param name="updateRequest">The request with updated statistics.</param>
    /// <returns>Returns the result of the update operation.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdatePlayerStatistic(Guid id, UpdatePlayerStatisticRequest updateRequest)
    {
        PlayerStatistic? existingStatistic = await _playerStatisticService.GetPlayerStatisticByIdAsync(id);

        if (existingStatistic is null)
        {
            return BadRequest($"Player statistic with id {id} not found.");
        }

        _mapper.Map(updateRequest, existingStatistic);
        bool updateResult = await _playerStatisticService.UpdatePlayerStatisticAsync(existingStatistic);

        return !updateResult ? BadRequest("Failed to update the player statistic.") : NoContent();
    }

    /// <summary>
    /// Deletes a player statistic by its id.
    /// </summary>
    /// <param name="id">The id of the player statistic to delete.</param>
    /// <returns>Returns the result of the delete operation.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> DeletePlayerStatisticById(Guid id)
    {
        PlayerStatistic? statistic = await _playerStatisticService.GetPlayerStatisticByIdAsync(id);

        if (statistic is null)
        {
            return BadRequest($"Player statistic with id {id} not found.");
        }

        bool deleteResult = await _playerStatisticService.DeletePlayerStatisticAsync(statistic);
        return deleteResult ? BadRequest($"Failed to delete player statistic with id: {id}.") : NoContent();
    }
}
