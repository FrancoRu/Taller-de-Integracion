using API.Utils;

using Application.DTOs.Champions.Response;
using Application.Interfaces.Services;

using Domain.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Controllers;

/// <summary>
/// Exposes the club's champions history across finished tournaments; reads are public.
/// </summary>
/// <param name="championService">The champion service.</param>
[Route("api/champions")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
public class ChampionController(IChampionService championService) : ControllerBase
{
    /// <summary>
    /// Lists each division's first-place champion across every finished tournament, optionally scoped to one season, omitting divisions without a decided champion.
    /// </summary>
    /// <param name="seasonId">Optional season filter as a GUID; when omitted, spans all seasons.</param>
    /// <returns>
    /// Returns 200 OK with the champions history.
    /// </returns>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ChampionHistoryResponse>))]
    public async Task<ActionResult<List<ChampionHistoryResponse>>> GetChampionsHistory([FromQuery] Guid? seasonId)
    {
        List<ChampionHistoryResponse> history = await championService.GetChampionsHistoryAsync(seasonId);
        return Ok(history);
    }
}
