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
/// Controller exposing the club's champions history across finished
/// tournaments. Reads are public.
/// </summary>
/// <param name="championService">The champion service.</param>
[Route("api/champions")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
public class ChampionController(IChampionService championService) : ControllerBase
{
    /// <summary>
    /// Lists the champion (1st place) of every division of every FINISHED
    /// tournament, optionally scoped to a single season. Divisions without a
    /// decided champion are omitted.
    /// </summary>
    /// <param name="seasonId">Optional season filter (GUID); when omitted, spans all seasons.</param>
    /// <returns>
    /// Returns 200 (OK) with the champions history.
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
