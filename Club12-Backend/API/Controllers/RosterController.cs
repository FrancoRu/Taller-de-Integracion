using Application.DTOs.Roster.Request;
using Application.DTOs.Roster.Response;
using Application.Interfaces.Services;

using Domain.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Threading.Tasks;

namespace API.Controllers;

/// <summary>
/// Controller for team roster operations. Writes require Owner or Admin.
/// </summary>
/// <param name="rosterCopyService">The service that clones rosters across seasons.</param>
[Route("api/teams/")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
public class RosterController(
    IRosterCopyService rosterCopyService
    ) : ControllerBase
{
    /// <summary>
    /// Clones a roster from a previous season's team into this team for a new
    /// season (HU-53): a fresh season registration is created for every source
    /// player, reusing the same Player rows. Medical records start Pending
    /// (HU-59) and sanctions are not carried over. Idempotent — a source player
    /// already registered to the target season is skipped.
    /// </summary>
    /// <param name="id">The target team to copy the roster into.</param>
    /// <param name="request">The source team + season and the target season.</param>
    /// <returns>
    /// <para>Returns 200 (OK) with how many registrations were created/skipped.</para>
    /// <para>Returns 403 (Forbidden) if the user is not authorized.</para>
    /// </returns>
    [Authorize(Roles = Roles.AdminOrOwner)]
    [HttpPost("{id:guid}/roster/copy")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RosterCopyResult))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RosterCopyResult>> CopyRoster(Guid id, [FromBody] CopyRosterRequest request)
    {
        RosterCopyResult result = await rosterCopyService.CopyRosterAsync(
            request.SourceTeamId, request.SourceTournamentId, id, request.TargetTournamentId);

        return Ok(result);
    }
}
