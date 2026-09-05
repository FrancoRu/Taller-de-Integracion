using Application.DTOs.Abstract.Response;
using Application.DTOs.MatchSeries.Request;
using Application.DTOs.MatchSeries.Response;
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
/// Manages best-of-N playoff series between two teams; reads are public but writes require Owner or Admin.
/// </summary>
/// <param name="matchSeriesService">Service for series business logic and persistence operations.</param>
/// <param name="mapper">AutoMapper instance for mapping between entities and DTOs.</param>
[Route("api/match-series/")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
public class MatchSeriesController(IMatchSeriesService matchSeriesService, IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Creates a new best-of-N series between two teams at a stage.
    /// </summary>
    /// <param name="request">The series creation request DTO.</param>
    /// <returns>The created series response.</returns>
    [HttpPost()]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(MatchSeriesResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MatchSeriesResponse>> CreateSeries(CreateMatchSeriesRequest request)
    {
        MatchSeries createdSeries = await matchSeriesService.CreateSeriesAsync(request.StageId, request.HomeTeamId, request.VisitorTeamId);
        MatchSeriesResponse response = mapper.Map<MatchSeriesResponse>(createdSeries);
        return CreatedAtAction(nameof(GetSeriesById), new { id = response.Id }, response);
    }

    /// <summary>
    /// Retrieves a series by its id.
    /// </summary>
    /// <param name="id">The id of the series.</param>
    /// <returns>The series entity, including its games.</returns>
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MatchSeriesResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MatchSeriesResponse>> GetSeriesById(Guid id)
    {
        MatchSeries? series = await matchSeriesService.GetSeriesByIdAsync(id);

        return series is null ? (ActionResult<MatchSeriesResponse>) NotFound(ErrorMessages.MatchSeries.NotFoundById(id)) : (ActionResult<MatchSeriesResponse>) Ok(mapper.Map<MatchSeriesResponse>(series));
    }

    /// <summary>
    /// Retrieves filtered and paginated series.
    /// </summary>
    /// <param name="filterRequest">The filter and pagination parameters.</param>
    /// <returns>Paginated list of series.</returns>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<MatchSeriesResponse>))]
    public async Task<ActionResult<PaginatedResponse<MatchSeriesResponse>>> GetFilteredSeries([FromQuery] GetMatchSeriesFilteredRequest filterRequest)
    {
        PaginatedResponse<MatchSeries> paginatedSeries = await matchSeriesService.GetAllSeriesAsync(filterRequest);
        PaginatedResponse<MatchSeriesResponse> response = mapper.Map<PaginatedResponse<MatchSeriesResponse>>(paginatedSeries);
        return Ok(response);
    }

    /// <summary>
    /// Schedules the next game of an existing series.
    /// </summary>
    /// <param name="id">The id of the series to add a game to.</param>
    /// <param name="request">The game scheduling request.</param>
    /// <returns>The created game.</returns>
    [HttpPost("{id:guid}/games")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SeriesGameResponse>> AddGameToSeries(Guid id, AddGameToSeriesRequest request)
    {
        Match game = await matchSeriesService.AddGameToSeriesAsync(id, request.MatchDate, request.VenueId);
        return CreatedAtAction(nameof(GetSeriesById), new { id }, mapper.Map<SeriesGameResponse>(game));
    }
}
