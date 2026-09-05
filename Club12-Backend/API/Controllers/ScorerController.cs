using Application.DTOs.Abstract.Response;
using Application.DTOs.Match.Request;
using Application.DTOs.Scorer.Request;
using Application.DTOs.Scorer.Response;
using Application.Interfaces.Mappers;
using Application.Interfaces.Services;

using Domain.Entities.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Threading.Tasks;

namespace API.Controllers;

/// <summary>
/// Read-only scorer ranking aggregations, always public: goals by team and by player.
/// </summary>
[ApiController]
[Route("api/[controller]/")]
[AllowAnonymous]
#pragma warning disable S6960
public class ScorerController(IScorerService scorerService, IMatchService matchService, IScorerMapper mapper) : ControllerBase
#pragma warning restore S6960
{
    /// <summary>
    /// Ranks teams by goals scored across the filtered matches, the Goleadores-by-team view.
    /// </summary>
    /// <param name="filter">The match filtering and pagination parameters.</param>
    /// <returns>A paginated team scorer ranking.</returns>
    [HttpGet("by-team")]
    public async Task<ActionResult> GetFilteredScorerByTeam([FromQuery] GetMatchesFilteredRequest filter)
    {
        PaginatedResponse<Match> matches = await matchService.GetAllMatchesAsync(filter);

        PaginatedResponse<ScorerByTeamResponse> response = mapper.FromPaginatedMatchToPaginatedScorerByTeamResponse(matches);

        return Ok(response);
    }

    /// <summary>
    /// Ranks individual players by goals scored, the Goleadores-by-player view.
    /// </summary>
    /// <param name="filter">The scorer filtering and pagination parameters.</param>
    /// <returns>A paginated player scorer ranking.</returns>
    [HttpGet("by-player")]
    public async Task<ActionResult> GetFilteredScorerByPlayer([FromQuery] GetScorerFilteredRequest filter)
    {
        PaginatedResponse<ScorerByPlayerResponse> response = await scorerService.GetAllScorersByPlayerAsync(filter);

        return Ok(response);
    }
}
