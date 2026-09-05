using API.Utils;

using Application.DTOs.Abstract.Response;
using Application.DTOs.Player.Request;
using Application.DTOs.Player.Response;
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
/// Controller for managing Players. Public reads return minimal data;
/// full player details and every write require staff roles.
/// </summary>
/// <param name="playerService">The Player service.</param>
/// <param name="teamService">The Team service.</param>
/// <param name="mapper">The AutoMapper instance.</param>
[Route("api/players/")]
[ApiController]
public class PlayerController(
    IPlayerService playerService,
    ITeamService teamService,
    IMapper mapper
    ) : ControllerBase
{
    /// <summary>
    /// Creates a player under the specified team. Returns the full admin-facing player
    /// response (not the trimmed public shape returned by the public endpoints).
    /// </summary>
    /// <param name="playerRequest">The player request.</param>
    /// <returns>The created Player response.
    /// <para>Returns 201 (Created) with the Player response if the creation was successful.</para>
    /// <para>Returns 400 (Bad Request) if the team was not found or is not linked to a tournament.</para>
    /// <para>Returns 403 (Forbidden) if the user lacks the Admin or Owner role.</para>
    /// </returns>
    [Authorize(Roles = Roles.AdminOrOwner)]
    [HttpPost()]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(AdminPlayerResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminPlayerResponse>> CreatePlayerAsync(CreatePlayerRequest playerRequest)
    {
        Guid teamId = playerRequest.TeamId;
        Team? existingTeam = await teamService.GetTeamByIdAsync(teamId);

        if (existingTeam is null)
        {
            return BadRequest(ErrorMessages.Team.NotFound(teamId));
        }

        if (existingTeam.TournamentId is null)
        {
            return BadRequest(ErrorMessages.Team.NotInTournament(teamId));
        }

        Player mappedPlayer = mapper.Map<Player>(playerRequest);
        Player createdPlayer = await playerService.CreatePlayerAsync(mappedPlayer, existingTeam.TournamentId.Value);
        AdminPlayerResponse playerResponse = mapper.Map<AdminPlayerResponse>(createdPlayer);
        return CreatedAtRoute("GetPlayerById", new { idOrSlug = createdPlayer.Id }, playerResponse);
    }

    /// <summary>
    /// Registers a player onto a team's roster for a tournament season,
    /// optionally assigning a dorsal, enforcing the HU-54 roster invariants
    /// (no two teams in one tournament, roster-size cap, unique dorsal).
    /// </summary>
    /// <param name="playerId">The player to register.</param>
    /// <param name="request">The team, tournament and optional dorsal.</param>
    /// <returns>
    /// <para>Returns 200 (OK) with the registration outcome.</para>
    /// <para>Returns 409 (Conflict) if a roster invariant is violated.</para>
    /// <para>Returns 403 (Forbidden) if the user is not authorized.</para>
    /// </returns>
    [Authorize(Roles = Roles.AdminOrOwner)]
    [HttpPost("{playerId:guid}/registration")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PlayerRegistrationResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlayerRegistrationResponse>> RegisterPlayerToTeam(
        Guid playerId, [FromBody] RegisterPlayerToTeamRequest request)
    {
        PlayerTeamRegistration registration = await playerService.RegisterPlayerToTeamAsync(
            playerId, request.TeamId, request.TournamentId, request.JerseyNumber);

        return Ok(mapper.Map<PlayerRegistrationResponse>(registration));
    }

    /// <summary>
    /// Retrieves a player by its id or its public slug.
    /// </summary>
    /// <param name="idOrSlug">The id (GUID) or slug of the player to retrieve.</param>
    /// <returns>The Player with the specified id or slug.
    /// <para>Returns 200 (OK) with the Player response if it was found.</para>
    /// <para>Returns 404 (Not Found) if the Player with the provided id or slug was not found.</para>
    /// </returns>
    [AllowAnonymous]
    [HttpGet("{idOrSlug}", Name = "GetPlayerById")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PublicPlayerResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicPlayerResponse>> GetPlayerByIdAsync(string idOrSlug)
    {
        Player? player = await playerService.GetPlayerByIdOrSlugAsync(idOrSlug);

        if (player is null)
        {
            return this.NotFoundProblem(nameof(Player), idOrSlug);
        }

        PublicPlayerResponse playerResponse = mapper.Map<PublicPlayerResponse>(player);
        return Ok(playerResponse);
    }

    /// <summary>
    /// Retrieves a player by its id or its slug, with complete data for the admin view.
    /// </summary>
    /// <param name="idOrSlug">The id (GUID) or exact slug of the player to retrieve.</param>
    /// <returns>The player's complete admin data.
    /// <para>Returns 200 (OK) with the <see cref="AdminPlayerResponse"/> if the player was found.</para>
    /// <para>Returns 404 (Not Found) if no player matches the provided id or slug.</para>
    /// </returns>
    [Authorize(Roles = Roles.AdminOrOwner)]
    [HttpGet("admin/{idOrSlug}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AdminPlayerResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminPlayerResponse>> GetPlayerByIdCompleteDataAsync(string idOrSlug)
    {
        Player? player = await playerService.GetPlayerByIdOrSlugAsync(idOrSlug);

        if (player is null)
        {
            return this.NotFoundProblem(nameof(Player), idOrSlug);
        }

        AdminPlayerResponse playerResponse = mapper.Map<AdminPlayerResponse>(player);
        return Ok(playerResponse);
    }

    /// <summary>
    /// Updates a player by its id.
    /// </summary>
    /// <param name="id">The id of the player to update.</param>
    /// <param name="playerRequest">The player request.</param>
    /// <returns>
    /// Returns 200 (OK) with the updated player if the update was successful.
    /// Returns 400 (Bad Request) if the player's team was not found or is not linked to a tournament.
    /// Returns 404 (Not Found) if no player matches the provided id.
    /// Returns 403 (Forbidden) if the user lacks the Admin or Owner role.
    /// </returns>
    [Authorize(Roles = Roles.AdminOrOwner)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdatePlayerAsync(Guid id, UpdatePlayerRequest playerRequest)
    {
        Player? existingPlayer = await playerService.GetPlayerByIdAsync(id);

        if (existingPlayer is null)
        {
            return this.NotFoundProblem(nameof(Player), id);
        }

        mapper.Map(playerRequest, existingPlayer);

        Team? currentTeam = await teamService.GetTeamByIdAsync(existingPlayer.TeamId);

        if (currentTeam is null)
        {
            return BadRequest(ErrorMessages.Team.NotFound(existingPlayer.TeamId));
        }

        if (currentTeam.TournamentId is null)
        {
            return BadRequest(ErrorMessages.Team.NotInTournament(existingPlayer.TeamId));
        }

        await playerService.UpdatePlayerAsync(existingPlayer, currentTeam.TournamentId.Value);

        return Ok(existingPlayer);
    }

    /// <summary>
    /// Deletes a player by its id.
    /// </summary>
    /// <param name="id">The id of the Player to delete.</param>
    /// <returns>
    /// Returns 204 (No Content) if the Player was successfully deleted.
    /// Returns 403 (Forbidden) if the user lacks the Admin or Owner role.
    /// </returns>
    [Authorize(Roles = Roles.AdminOrOwner)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeletePlayerByIdAsync(Guid id)
    {
        await playerService.DeletePlayerAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Retrieves filtered players with pagination.
    /// </summary>
    /// <param name="filterRequest">The filtering and pagination parameters.</param>
    /// <returns>A paginated response containing the filtered players.</returns>
    [AllowAnonymous]
    [HttpGet("public")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<PublicPlayerResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<PublicPlayerResponse>>> GetFilteredPlayersAsync([FromQuery] PlayerFilterRequestBase filterRequest)
    {
        PaginatedResponse<Player> paginatedPlayers = await playerService.GetAllPlayersAsync(filterRequest);

        PaginatedResponse<PublicPlayerResponse> response = mapper.Map<PaginatedResponse<PublicPlayerResponse>>(paginatedPlayers);

        return Ok(response);
    }

    /// <summary>
    /// Retrieves filtered players with pagination and detailed information for admins.
    /// This endpoint is for private use only and requires admin access.
    /// </summary>
    /// <param name="filterRequest">The filtering and pagination parameters.</param>
    /// <returns>A paginated response containing the filtered players.</returns>
    /// <response code="200">Returns a paginated list of filtered players</response>
    /// <response code="400">Returns 400 if there is an invalid filter parameter or the filter results in no data</response>
    /// <response code="403">Returns 403 if the user does not have the required permissions (admin)</response>
    [Authorize(Roles = Roles.AdminOrOwner)]
    [HttpGet("")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<AdminPlayerResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResponse<AdminPlayerResponse>>> GetFilteredPlayersPrivateAsync([FromQuery] GetPlayersFilteredRequest filterRequest)
    {
        PaginatedResponse<Player> paginatedPlayers = await playerService.GetAllPlayersAsync(filterRequest);

        PaginatedResponse<AdminPlayerResponse> response = mapper.Map<PaginatedResponse<AdminPlayerResponse>>(paginatedPlayers);

        return Ok(response);
    }
}
