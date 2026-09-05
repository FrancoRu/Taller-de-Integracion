using API.Utils;

using Application.DTOs.Club.Response;
using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System.Threading.Tasks;

namespace API.Controllers;

/// <summary>
/// Manages the stable cross-season club identity; reading a club's history is public while the idempotent backfill is a staff maintenance action.
/// </summary>
/// <param name="clubService">The club service.</param>
[Route("api/clubs/")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
public class ClubController(IClubService clubService) : ControllerBase
{
    /// <summary>
    /// Retrieves a club and its trajectory across seasons, including the per-season teams that belong to it and the tournaments each was registered in.
    /// </summary>
    /// <param name="idOrSlug">The club's GUID id or its slug.</param>
    /// <returns>
    /// <para>Returns 200 OK with the club history when found.</para>
    /// <para>Returns 404 Not Found when no club matches.</para>
    /// </returns>
    [AllowAnonymous]
    [HttpGet("{idOrSlug}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ClubHistoryResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClubHistoryResponse>> GetClubHistory(string idOrSlug)
    {
        ClubHistoryResponse? history = await clubService.GetClubHistoryAsync(idOrSlug);

        if (history is null)
        {
            return this.NotFoundProblem(nameof(Club), idOrSlug);
        }

        return Ok(history);
    }

    /// <summary>
    /// Idempotently links every unlinked team to a stable club, creating clubs as needed; safe to re-run since a second call reports zeros.
    /// </summary>
    /// <returns>Returns 200 OK with how many clubs were created and teams linked.</returns>
    [HttpPost("backfill")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ClubBackfillResult))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ClubBackfillResult>> BackfillClubs()
    {
        ClubBackfillResult result = await clubService.BackfillClubsAsync();
        return Ok(result);
    }
}
