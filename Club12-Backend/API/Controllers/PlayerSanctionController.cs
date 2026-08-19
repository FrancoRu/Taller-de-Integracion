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
using System.Threading.Tasks;

namespace API.Controllers;

/// <summary>
/// Controller for managing Player Sanctions. Reads are public; writes
/// require Owner or TournamentManager.
/// </summary>
/// <param name="playerSanctionService">The Player Sanction service.</param>
/// <param name="mapper">The AutoMapper instance.</param>
[Route("api/player-sanctions/")]
[ApiController]
[Authorize(Roles = Roles.AdminOwnerOrTournamentManager)]
public class PlayerSanctionController(IPlayerSanctionService playerSanctionService, IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Creates a new player sanction.
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
        PlayerSanctionResponse sanctionResponse = mapper.Map<PlayerSanctionResponse>(createdSanction);

        return CreatedAtAction(nameof(GetPlayerSanctionById), new { idOrSlug = sanctionResponse.Id }, sanctionResponse);
    }

    /// <summary>
    /// Retrieves a player sanction by its id or its slug.
    /// </summary>
    /// <param name="idOrSlug">Player sanction identifier (GUID) or slug.</param>
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

        PlayerSanctionResponse sanctionResponse = mapper.Map<PlayerSanctionResponse>(sanction);
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

        return Ok(response);
    }
    /// <summary>
    /// Updates a player sanction asynchronously.
    /// </summary>
    /// <param name="id">The id of the sanction to update.</param>
    /// <param name="updateRequest">The request with updated sanction data.</param>
    /// <returns>Returns the result of the update operation.</returns>
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
        return Ok(mapper.Map<PlayerSanctionResponse>(existingSanction));
    }

    /// <summary>
    /// Submits an appeal against a player sanction.
    /// </summary>
    /// <param name="id">The id of the sanction being appealed.</param>
    /// <param name="appealRequest">The appeal reason.</param>
    /// <returns>The updated player sanction response.</returns>
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
        return Ok(mapper.Map<PlayerSanctionResponse>(existingSanction));
    }

    /// <summary>
    /// Resolves a pending appeal, recording the decision.
    /// </summary>
    /// <param name="id">The id of the sanction whose appeal is resolved.</param>
    /// <param name="resolveRequest">The decision (accepted/rejected) and resolution notes.</param>
    /// <returns>The updated player sanction response.</returns>
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
        return Ok(mapper.Map<PlayerSanctionResponse>(existingSanction));
    }

    /// <summary>
    /// Deletes a player sanction by its id.
    /// </summary>
    /// <param name="id">The id of the player sanction to delete.</param>
    /// <returns>Returns the result of the delete operation.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> DeletePlayerSanctionById(Guid id)
    {
        await playerSanctionService.DeletePlayerSanctionAsync(id);
        return NoContent();
    }
}
