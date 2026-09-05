using API.Utils;

using Application.DTOs.Statistics.Response;
using Application.Interfaces.Services;

using Domain.Entities.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Threading.Tasks;

namespace API.Controllers;

/// <summary>
/// Read-only historical player statistics; always public.
/// </summary>
[ApiController]
[Route("api/statistics/")]
[AllowAnonymous]
public class StatisticsController(IStatisticsService statisticsService) : ControllerBase
{
    /// <summary>
    /// A player's statistic card: total and average points and games played, per season and overall.
    /// </summary>
    /// <param name="playerId">The player's id.</param>
    [HttpGet("players/{playerId:guid}/card")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PlayerStatisticCardResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerStatisticCardResponse>> GetPlayerCard(Guid playerId)
    {
        PlayerStatisticCardResponse? card = await statisticsService.GetPlayerCardAsync(playerId);

        if (card is null)
        {
            return this.NotFoundProblem(nameof(Player), playerId);
        }

        return Ok(card);
    }

    /// <summary>
    /// A player's trajectory across seasons: team, stats, and sanctions for each season.
    /// </summary>
    /// <param name="playerId">The player's id.</param>
    [HttpGet("players/{playerId:guid}/history")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PlayerHistoryResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerHistoryResponse>> GetPlayerHistory(Guid playerId)
    {
        PlayerHistoryResponse? history = await statisticsService.GetPlayerHistoryAsync(playerId);

        if (history is null)
        {
            return this.NotFoundProblem(nameof(Player), playerId);
        }

        return Ok(history);
    }
}
