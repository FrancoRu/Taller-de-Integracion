using API.Utils;

using Application.DTOs.Club.Request;
using Application.DTOs.Club.Response;
using Application.Interfaces.Services;

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

    /// <summary>
    /// Retrieves every club's stable identity summary, used to populate the "link to parent club" picker.
    /// </summary>
    /// <returns>Returns 200 OK with every club, ordered by name.</returns>
    [HttpGet()]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ClubSummaryResponse>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ClubSummaryResponse>>> GetAllClubs()
    {
        IEnumerable<ClubSummaryResponse> clubs = await clubService.GetAllClubsAsync();
        return Ok(clubs);
    }

    /// <summary>
    /// Renames a club. The club's slug never changes, so its public URL stays stable.
    /// </summary>
    /// <param name="id">The club to rename.</param>
    /// <param name="request">The new display name.</param>
    /// <returns>
    /// <para>Returns 200 OK with the club's updated history when renamed.</para>
    /// <para>Returns 404 Not Found when the club doesn't exist.</para>
    /// <para>Returns 409 Conflict when the name is blank.</para>
    /// </returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ClubHistoryResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClubHistoryResponse>> RenameClub(Guid id, [FromBody] RenameClubRequest request)
    {
        ClubHistoryResponse history = await clubService.RenameClubAsync(id, request.Name);
        return Ok(history);
    }

    /// <summary>
    /// Links a club as a squad of a parent institution club, so both are shown together as one institution.
    /// </summary>
    /// <param name="id">The squad club to link.</param>
    /// <param name="request">The parent institution club it becomes a squad of.</param>
    /// <returns>
    /// <para>Returns 200 OK with the club's updated history when linked.</para>
    /// <para>Returns 404 Not Found when either club doesn't exist.</para>
    /// <para>Returns 409 Conflict when the link would create a chain deeper than institution -> squads.</para>
    /// </returns>
    [HttpPut("{id:guid}/parent")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ClubHistoryResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClubHistoryResponse>> LinkClubParent(Guid id, [FromBody] LinkClubParentRequest request)
    {
        ClubHistoryResponse history = await clubService.LinkClubToParentAsync(id, request.ParentClubId);
        return Ok(history);
    }

    /// <summary>
    /// Clears a club's parent institution link, if any.
    /// </summary>
    /// <param name="id">The squad club to unlink.</param>
    /// <returns>
    /// <para>Returns 200 OK with the club's updated history.</para>
    /// <para>Returns 404 Not Found when the club doesn't exist.</para>
    /// </returns>
    [HttpDelete("{id:guid}/parent")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ClubHistoryResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClubHistoryResponse>> UnlinkClubParent(Guid id)
    {
        ClubHistoryResponse history = await clubService.UnlinkClubParentAsync(id);
        return Ok(history);
    }
}
