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
/// Read-only historical player statistics (HU-87 / HU-88). Always public, like
/// the goleadores ranking. The three ranking scopes of HU-85 (per tournament,
/// per season, all-time) are served by the goleadores endpoint
/// (api/Scorer/by-player) via its TournamentId / Season query parameters; these
/// endpoints add the per-player card and cross-season history that link to it.
/// </summary>
[ApiController]
[Route("api/statistics/")]
[AllowAnonymous]
public class StatisticsController(IStatisticsService statisticsService) : ControllerBase
{
    /// <summary>
    /// HU-87: a player's statistic card — total and average points and games
    /// played, per season and overall.
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
    /// HU-88: a player's trajectory across seasons — for each season, the team
    /// they were on, their stats, and their sanctions.
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
