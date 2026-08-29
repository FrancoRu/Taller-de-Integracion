using API.Utils;

using Application.DTOs.Season.Request;
using Application.DTOs.Season.Response;
using Application.Interfaces.Services;

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
/// Controller for managing Seasons ("Temporadas"), the top-level grouping that
/// gathers several tournaments of the same period. Reads are public; writes
/// require Owner or Admin.
/// </summary>
/// <param name="seasonService">The Season service.</param>
/// <param name="mapper">The AutoMapper instance.</param>
[Route("api/seasons/")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
public class SeasonController(ISeasonService seasonService, IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Creates a new season asynchronously.
    /// </summary>
    /// <param name="seasonRequest">The season creation request.</param>
    /// <returns>The created Season response.
    /// <para>Returns 201 (Created) with the Season response if the creation was successful.</para>
    /// <para>Returns 403 (Forbidden) if the user is not authorized.</para>
    /// </returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(SeasonResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SeasonResponse>> CreateSeason(CreateSeasonRequest seasonRequest)
    {
        Season season = mapper.Map<Season>(seasonRequest);
        await seasonService.CreateSeasonAsync(season);
        SeasonResponse seasonResponse = mapper.Map<SeasonResponse>(season);
        return CreatedAtAction(nameof(GetSeasonById), new { idOrSlug = seasonResponse.Id }, seasonResponse);
    }

    /// <summary>
    /// Retrieves a season by its id or its public slug asynchronously.
    /// </summary>
    /// <param name="idOrSlug">The id (GUID) or slug of the season to retrieve.</param>
    /// <returns>The Season with the specified id or slug.
    /// <para>Returns 200 (OK) with the Season response if it was found.</para>
    /// <para>Returns 404 (Not Found) if the Season with the provided id or slug was not found.</para>
    /// </returns>
    [AllowAnonymous]
    [HttpGet("{idOrSlug}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SeasonResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SeasonResponse>> GetSeasonById(string idOrSlug)
    {
        Season? season = await seasonService.GetSeasonByIdOrSlugAsync(idOrSlug);

        if (season is null)
        {
            return this.NotFoundProblem(nameof(Season), idOrSlug);
        }

        SeasonResponse seasonResponse = mapper.Map<SeasonResponse>(season);
        return Ok(seasonResponse);
    }

    /// <summary>
    /// Updates a season by its id asynchronously.
    /// </summary>
    /// <param name="id">The id of the season to update.</param>
    /// <param name="seasonRequest">The season update request.</param>
    /// <returns>
    /// Returns 204 (No Content) if the update was successful.
    /// Returns 404 (Not Found) if the Season with the provided id was not found.
    /// Returns 403 (Forbidden) if the user is not authorized.
    /// </returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdateSeason(Guid id, UpdateSeasonRequest seasonRequest)
    {
        Season? existingSeason = await seasonService.GetSeasonByIdAsync(id);

        if (existingSeason is null)
        {
            return this.NotFoundProblem(nameof(Season), id);
        }

        mapper.Map(seasonRequest, existingSeason);

        await seasonService.UpdateSeasonAsync(existingSeason);

        return NoContent();
    }

    /// <summary>
    /// Deletes a season by its id asynchronously.
    /// </summary>
    /// <param name="id">The id of the Season to delete.</param>
    /// <returns>
    /// Returns 204 (No Content) if the Season was successfully deleted.
    /// Returns 404 (Not Found) if the Season with the provided id was not found.
    /// Returns 403 (Forbidden) if the user is not authorized.
    /// </returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteSeasonById(Guid id)
    {
        Season? season = await seasonService.GetSeasonByIdAsync(id);

        if (season is null)
        {
            return this.NotFoundProblem(nameof(Season), id);
        }

        await seasonService.DeleteSeasonAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Retrieves all seasons asynchronously.
    /// </summary>
    /// <returns>A list of all Season responses.</returns>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<SeasonResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<SeasonResponse>>> GetAllSeasons()
    {
        IEnumerable<Season> seasons = await seasonService.GetAllSeasonsAsync();
        IEnumerable<SeasonResponse> response = mapper.Map<IEnumerable<SeasonResponse>>(seasons);

        return Ok(response);
    }
}
