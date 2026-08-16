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
/// Read-only scorer ranking aggregations. Always public.
/// </summary>
[ApiController]
[Route("api/[controller]/")]
[AllowAnonymous]
public class ScorerController(IScorerService scorerService, IMatchService matchService, IScorerMapper mapper) : ControllerBase
{
    [HttpGet("by-team")]
    public async Task<ActionResult> GetFilteredScorerByTeam([FromQuery] GetMatchesFilteredRequest filter)
    {
        PaginatedResponse<Match> matches = await matchService.GetAllMatchesAsync(filter);

        PaginatedResponse<ScorerByTeamResponse> response = mapper.FromPaginatedMatchToPaginatedScorerByTeamResponse(matches);

        return Ok(response);
    }

    [HttpGet("by-player")]
    public async Task<ActionResult> GetFilteredScorerByPlayer([FromQuery] GetScorerFilteredRequest filter)
    {
        PaginatedResponse<ScorerByPlayerResponse> response = await scorerService.GetAllScorersByPlayerAsync(filter);

        return Ok(response);
    }
}
