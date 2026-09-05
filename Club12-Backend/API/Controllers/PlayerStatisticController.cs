using API.Utils;

using Application.DTOs.Abstract.Response;
using Application.DTOs.PlayerStatistic.Request;
using Application.DTOs.PlayerStatistic.Response;
using Application.Interfaces.Services;

using AutoMapper;

using Domain.Entities.Models;
using Domain.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Controllers;

/// <summary>
/// Manages player statistics; reads are public but writes require Owner or Admin.
/// </summary>
/// <param name="playerStatisticService">The Player Statistic service.</param>
/// <param name="mapper">The Auto_mapper instance.</param>
[Route("api/player-statistics/")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
public class PlayerStatisticController(IPlayerStatisticService playerStatisticService, IMapper mapper) : ControllerBase
{

    /// <summary>
    /// Records a single player-statistic entry directly, without the roster, eligibility, or score-total validation used by the match-sheet endpoint.
    /// </summary>
    /// <param name="playerStatisticRequest">The player statistic request DTO.</param>
    /// <returns>The created player statistic response.</returns>
    [HttpPost()]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(PlayerStatisticResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PlayerStatisticResponse>> CreatePlayerStatistic(CreatePlayerStatisticRequest playerStatisticRequest)
    {
        PlayerStatistic mappedStatistic = mapper.Map<PlayerStatistic>(playerStatisticRequest);
        PlayerStatistic createdStatistic = await playerStatisticService.CreatePlayerStatisticAsync(mappedStatistic);
        PlayerStatisticResponse statisticResponse = mapper.Map<PlayerStatisticResponse>(createdStatistic);

        return new ObjectResult(statisticResponse) { StatusCode = StatusCodes.Status201Created };
    }

    /// <summary>
    /// Loads a whole team's scoring sheet for a match, validating that per-player points sum to the team's final score and every player is rostered and eligible.
    /// </summary>
    /// <param name="request">The match, team, and per-player points.</param>
    /// <returns>The persisted Points statistics for the team in this match.</returns>
    [HttpPost("match-sheet")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(PaginatedResponse<PlayerStatisticResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> LoadMatchSheet(LoadMatchSheetRequest request)
    {
        List<PlayerStatistic> created = await playerStatisticService.LoadTeamMatchSheetAsync(request);

        List<PlayerStatisticResponse> response = mapper.Map<List<PlayerStatisticResponse>>(created);

        return new ObjectResult(response) { StatusCode = StatusCodes.Status201Created };
    }

    /// <summary>
    /// Retrieves a paginated, filtered list of player statistics.
    /// </summary>
    /// <param name="filterRequest">The filtering and pagination parameters.</param>
    /// <returns>A paginated response containing the filtered player statistics.</returns>
    [AllowAnonymous]
    [HttpGet()]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<PlayerStatisticResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<PlayerStatisticResponse>>> GetFilteredPlayerStatistics(
        [FromQuery] GetPlayerStatisticsFilteredRequest filterRequest)
    {
        PaginatedResponse<PlayerStatistic> paginatedStatistics =
            await playerStatisticService.GetPlayerStatisticsAsync(filterRequest);

        PaginatedResponse<PlayerStatisticResponse> response =
            mapper.Map<PaginatedResponse<PlayerStatisticResponse>>(paginatedStatistics);

        return Ok(response);
    }

    /// <summary>
    /// Retrieves a player statistic by its id.
    /// </summary>
    /// <param name="id">The id of the player statistic.</param>
    /// <returns>The player statistic response DTO.</returns>
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PlayerStatisticResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerStatisticResponse>> GetPlayerStatisticById(Guid id)
    {
        PlayerStatistic? statistic = await playerStatisticService.GetPlayerStatisticByIdAsync(id);

        if (statistic is null)
        {
            return this.NotFoundProblem(nameof(PlayerStatistic), id);
        }

        PlayerStatisticResponse statisticResponse = mapper.Map<PlayerStatisticResponse>(statistic);
        return Ok(statisticResponse);
    }

    /// <summary>
    /// Updates a player statistic by its id.
    /// </summary>
    /// <param name="id">The id of the statistic to update.</param>
    /// <param name="updateRequest">The request with updated statistics.</param>
    /// <returns>Returns 204 No Content on success, or 404 Not Found if no statistic matches the id.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdatePlayerStatistic(Guid id, UpdatePlayerStatisticRequest updateRequest)
    {
        PlayerStatistic? existingStatistic = await playerStatisticService.GetPlayerStatisticByIdAsync(id);

        if (existingStatistic is null)
        {
            return this.NotFoundProblem(nameof(PlayerStatistic), id);
        }

        mapper.Map(updateRequest, existingStatistic);
        await playerStatisticService.UpdatePlayerStatisticAsync(existingStatistic);

        return NoContent();
    }

    /// <summary>
    /// Deletes a player statistic by its id.
    /// </summary>
    /// <param name="id">The id of the player statistic to delete.</param>
    /// <returns>Returns 204 No Content on success.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> DeletePlayerStatisticById(Guid id)
    {
        await playerStatisticService.DeletePlayerStatisticAsync(id);
        return NoContent();
    }
}
