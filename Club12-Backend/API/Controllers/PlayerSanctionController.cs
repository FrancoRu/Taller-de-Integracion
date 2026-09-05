using API.Utils;

using Application.DTOs.Abstract.Response;
using Application.DTOs.PlayerSanction.Request;
using Application.DTOs.PlayerSanction.Response;
using Application.Interfaces.Services;
using Application.Utils.Constants;

using AutoMapper;

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
/// Manages player sanctions; reads are public but writes require Owner or Admin.
/// </summary>
/// <param name="playerSanctionService">The Player Sanction service.</param>
/// <param name="mapper">The AutoMapper instance.</param>
[Route("api/player-sanctions/")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
public class PlayerSanctionController(IPlayerSanctionService playerSanctionService, IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Creates a sanction against a player, team, or staff member, enriched with the subject's display name and fechas-based status.
    /// </summary>
    /// <param name="playerSanctionRequest">The player sanction request DTO.</param>
    /// <returns>The created player sanction response.</returns>
    [HttpPost()]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(PlayerSanctionResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PlayerSanctionResponse>> CreatePlayerSanction(CreatePlayerSanctionRequest playerSanctionRequest)
    {
        PlayerSanction mappedSanction = mapper.Map<PlayerSanction>(playerSanctionRequest);
        PlayerSanction createdSanction = await playerSanctionService.CreatePlayerSanctionAsync(mappedSanction);
        PlayerSanctionResponse sanctionResponse = await ToResponseAsync(createdSanction);

        return CreatedAtAction(nameof(GetPlayerSanctionById), new { idOrSlug = sanctionResponse.Id }, sanctionResponse);
    }

    /// <summary>
    /// Retrieves a player sanction by its id or its slug.
    /// </summary>
    /// <param name="idOrSlug">Player sanction identifier as a GUID or slug.</param>
    /// <returns>The player sanction response DTO.</returns>
    [AllowAnonymous]
    [HttpGet("{idOrSlug}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PlayerSanctionResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerSanctionResponse>> GetPlayerSanctionById(string idOrSlug)
    {
        PlayerSanction? sanction = await playerSanctionService.GetPlayerSanctionByIdOrSlugAsync(idOrSlug);

        if (sanction is null)
        {
            return this.NotFoundProblem(nameof(PlayerSanction), idOrSlug);
        }

        PlayerSanctionResponse sanctionResponse = await ToResponseAsync(sanction);
        return Ok(sanctionResponse);
    }

    /// <summary>
    /// Retrieves a paginated list of player sanctions filtered by the specified criteria.
    /// </summary>
    /// <param name="filterRequest">The filtering parameters for player sanctions.</param>
    /// <returns>
    /// Returns a PaginatedResponse{PlayerSanctionResponse} containing the filtered sanctions.
    /// Possible HTTP responses:
    /// <list type="bullet">
    ///   <item><description>200 OK - The filtered sanctions were retrieved successfully.</description></item>
    ///   <item><description>400 Bad Request - The request parameters were invalid.</description></item>
    /// </list>
    /// </returns>
    [AllowAnonymous]
    [HttpGet("find")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<PlayerSanctionResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResponse<PlayerSanctionResponse>>> GetFilteredPlayersPrivateAsync([FromQuery] GetPlayerSanctionsFilteredRequest filterRequest)
    {
        PaginatedResponse<PlayerSanction> paginatedPlayerSanctions = await playerSanctionService.GetPlayerSanctionsAsync(filterRequest);

        PaginatedResponse<PlayerSanctionResponse> response = mapper.Map<PaginatedResponse<PlayerSanctionResponse>>(paginatedPlayerSanctions);

        List<PlayerSanctionResponse> enrichedItems = [];
        foreach (PlayerSanction sanction in paginatedPlayerSanctions.Items)
        {
            enrichedItems.Add(await ToResponseAsync(sanction));
        }
        response.Items = enrichedItems;

        return Ok(response);
    }
    /// <summary>
    /// Updates a player sanction's fields and re-enriches the response with the subject's display name and fechas-based status.
    /// </summary>
    /// <param name="id">The id of the sanction to update.</param>
    /// <param name="updateRequest">The request with updated sanction data.</param>
    /// <returns>Returns 200 OK with the updated sanction, or 404 Not Found if no sanction matches the id.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PlayerSanctionResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdatePlayerSanction(Guid id, UpdatePlayerSanctionRequest updateRequest)
    {
        PlayerSanction? existingSanction = await playerSanctionService.GetPlayerSanctionByIdAsync(id);

        if (existingSanction is null)
        {
            return this.NotFoundProblem(nameof(PlayerSanction), id);
        }

        mapper.Map(updateRequest, existingSanction);
        await playerSanctionService.UpdatePlayerSanctionAsync(existingSanction);
        return Ok(await ToResponseAsync(existingSanction));
    }

    /// <summary>
    /// Submits an appeal against a sanction, moving it into pending review.
    /// </summary>
    /// <param name="id">The id of the sanction being appealed.</param>
    /// <param name="appealRequest">The appeal reason.</param>
    /// <returns>The updated sanction response. Returns 400 Bad Request if an appeal is already pending for this sanction.</returns>
    [HttpPut("{id:guid}/appeal")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PlayerSanctionResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerSanctionResponse>> AppealPlayerSanction(
        Guid id, AppealPlayerSanctionRequest appealRequest)
    {
        PlayerSanction? existingSanction = await playerSanctionService.GetPlayerSanctionByIdAsync(id);

        if (existingSanction is null)
        {
            return this.NotFoundProblem(nameof(PlayerSanction), id);
        }

        if (existingSanction.AppealStatus == SanctionAppealStatus.Pending)
        {
            return BadRequest(ErrorMessages.PlayerSanction.AppealAlreadyPending);
        }

        existingSanction.AppealStatus = SanctionAppealStatus.Pending;
        existingSanction.AppealReason = appealRequest.Reason;
        existingSanction.AppealDate = DateTime.UtcNow;
        existingSanction.AppealResolution = null;
        existingSanction.AppealResolvedDate = null;

        await playerSanctionService.UpdatePlayerSanctionAsync(existingSanction);
        return Ok(await ToResponseAsync(existingSanction));
    }

    /// <summary>
    /// Resolves a sanction's pending appeal as accepted or rejected, where accepting lifts the sanction immediately without deleting the sanction record.
    /// </summary>
    /// <param name="id">The id of the sanction whose appeal is resolved.</param>
    /// <param name="resolveRequest">The accepted or rejected decision and resolution notes.</param>
    /// <returns>The updated sanction response. Returns 400 Bad Request if the sanction has no pending appeal to resolve.</returns>
    [HttpPut("{id:guid}/appeal/resolve")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PlayerSanctionResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerSanctionResponse>> ResolvePlayerSanctionAppeal(
        Guid id, ResolveAppealRequest resolveRequest)
    {
        PlayerSanction? existingSanction = await playerSanctionService.GetPlayerSanctionByIdAsync(id);

        if (existingSanction is null)
        {
            return this.NotFoundProblem(nameof(PlayerSanction), id);
        }

        if (existingSanction.AppealStatus != SanctionAppealStatus.Pending)
        {
            return BadRequest(ErrorMessages.PlayerSanction.NoPendingAppealToResolve);
        }

        existingSanction.AppealStatus = resolveRequest.Accepted
            ? SanctionAppealStatus.Accepted
            : SanctionAppealStatus.Rejected;
        existingSanction.AppealResolution = resolveRequest.Resolution;
        existingSanction.AppealResolvedDate = DateTime.UtcNow;

        await playerSanctionService.UpdatePlayerSanctionAsync(existingSanction);
        return Ok(await ToResponseAsync(existingSanction));
    }

    /// <summary>
    /// Deletes a player sanction by its id.
    /// </summary>
    /// <param name="id">The id of the player sanction to delete.</param>
    /// <returns>Returns 204 No Content on success.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> DeletePlayerSanctionById(Guid id)
    {
        await playerSanctionService.DeletePlayerSanctionAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Maps a sanction to its response and enriches it with the fechas-based remaining/active status, labelled in fechas rather than calendar days.
    /// </summary>
    private async Task<PlayerSanctionResponse> ToResponseAsync(PlayerSanction sanction)
    {
        PlayerSanctionResponse response = mapper.Map<PlayerSanctionResponse>(sanction);

        // Resolve WHO the sanction targets (HU-77) so the list/detail always
        // show the subject — the player's name, the TEAM's name for a team
        // sanction, or the staff member's name — regardless of which
        // navigations the query happened to load.
        (string? playerFullName, string? teamName, string? staffName) =
            await playerSanctionService.ResolveSubjectAsync(sanction);
        response.PlayerFullName = playerFullName;
        response.TeamName = teamName;
        response.StaffName = staffName;

        int? fechasRemaining = await playerSanctionService.GetFechasRemainingAsync(sanction);
        response.FechasRemaining = fechasRemaining;
        response.IsActive = fechasRemaining.HasValue
            ? fechasRemaining.Value > 0
            : sanction.Duration > 0;

        return response;
    }
}
