using Application.DTOs.Abstract.Response;
using Application.DTOs.Match.Request;
using Application.DTOs.Scorer.Request;
using Application.DTOs.Scorer.Response;
using Application.Interfaces.Services;

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
public class ScorerController(IScorerService scorerService) : ControllerBase
{
    /// <summary>
    /// Ranks teams by goals scored across the filtered matches, the Goleadores-by-team view.
    /// </summary>
    /// <param name="filter">The match filtering and pagination parameters.</param>
    /// <returns>A paginated team scorer ranking.</returns>
    [HttpGet("by-team")]
    public async Task<ActionResult> GetFilteredScorerByTeam([FromQuery] GetMatchesFilteredRequest filter)
    {
        PaginatedResponse<ScorerByTeamResponse> response = await scorerService.GetAllScorersByTeamAsync(filter);

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
