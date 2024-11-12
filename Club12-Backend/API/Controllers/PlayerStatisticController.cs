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
/// <remarks>
/// Initializes a new instance of the <see cref="PlayerStatisticController"/> class.
/// </remarks>
/// <param name="playerStatisticService">The Player Statistic service.</param>
/// <param name="mapper">The AutoMapper instance.</param>
[Authorize(Roles = "SuperAdmin")]
[Route("api/player-statistics/")]
[ApiController]
public class PlayerStatisticController(IPlayerStatisticService playerStatisticService, IMapper mapper) : ControllerBase
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
    public ActionResult<PlayerStatisticResponse> CreatePlayerStatistic(CreatePlayerStatisticRequest playerStatisticRequest)
    {
        PlayerStatistic mappedStatistic = mapper.Map<PlayerStatistic>(playerStatisticRequest);
        PlayerStatistic createdStatistic = playerStatisticService.CreatePlayerStatistic(mappedStatistic);
        PlayerStatisticResponse statisticResponse = mapper.Map<PlayerStatisticResponse>(createdStatistic);

        return CreatedAtAction(nameof(GetPlayerStatisticById), new { id = statisticResponse.Id }, statisticResponse);
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
    public ActionResult<PlayerStatisticResponse> GetPlayerStatisticById(Guid id)
    {
        PlayerStatistic? statistic = playerStatisticService.GetPlayerStatisticById(id);

        if (statistic is null)
        {
            return BadRequest($"Player statistic with id {id} not found.");
        }

        PlayerStatisticResponse statisticResponse = mapper.Map<PlayerStatisticResponse>(statistic);
        return Ok(statisticResponse);
    }

    /// <summary>
    /// Updates a player statistic asynchronously.
    /// </summary>
    /// <param name="statisticId">The id of the statistic to update.</param>
    /// <param name="updateRequest">The request with updated statistics.</param>
    /// <returns>Returns the result of the update operation.</returns>
    [HttpPut("{statisticId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdatePlayerStatistic(Guid statisticId, UpdatePlayerStatisticRequest updateRequest)
    {
        PlayerStatistic? existingStatistic = playerStatisticService.GetPlayerStatisticById(statisticId);

        if (existingStatistic is null)
        {
            return BadRequest($"Player statistic with id {statisticId} not found.");
        }

        mapper.Map(updateRequest, existingStatistic);
        bool updateResult = await playerStatisticService.UpdatePlayerStatisticAsync(existingStatistic);

        return !updateResult ? BadRequest("Failed to update the player statistic.") : Ok();
    }

    /// <summary>
    /// Deletes a player statistic by its id.
    /// </summary>
    /// <param name="id">The id of the player statistic to delete.</param>
    /// <returns>Returns the result of the delete operation.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult DeletePlayerStatisticById(Guid id)
    {
        PlayerStatistic? statistic = playerStatisticService.GetPlayerStatisticById(id);

        if (statistic is null)
        {
            return BadRequest($"Player statistic with id {id} not found.");
        }

        playerStatisticService.DeletePlayerStatistic(statistic);
        return Ok();
    }
}
