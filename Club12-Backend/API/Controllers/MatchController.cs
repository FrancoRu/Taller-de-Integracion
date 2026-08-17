using API.Utils;

using Application.DTOs.Abstract.Response;
using Application.DTOs.Match.Request;
using Application.DTOs.Match.Response;
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
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers;

/// <summary>
/// Controller for managing Matches. Reads are public; writes require
/// Owner or TournamentManager.
/// </summary>
/// <param name="matchService">The Match service.</param>
/// <param name="stageTeamMatchService">The stage-team match service.</param>
/// <param name="matchSeriesService">The playoff series service.</param>
/// <param name="mapper">The AutoMapper instance.</param>
[Route("api/matches/")]
[ApiController]
[Authorize(Roles = Roles.OwnerOrTournamentManager)]
public class MatchController(IMatchService matchService, IStageTeamMatchService stageTeamMatchService, IMatchSeriesService matchSeriesService, IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Creates a new match.
    /// </summary>
    /// <param name="matchRequest">The match request DTO.</param>
    /// <returns>The created match response.</returns>
    [HttpPost()]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(MinimalMatchResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MinimalMatchResponse>> CreateMatch(CreateMatchRequest matchRequest)
    {
        Match mappedMatch = mapper.Map<Match>(matchRequest);
        Match createdMatch = await matchService.CreateMatchAsync(mappedMatch);
        MinimalMatchResponse matchResponse = mapper.Map<MinimalMatchResponse>(createdMatch);

        return new ObjectResult(matchResponse) { StatusCode = StatusCodes.Status201Created };
    }

    /// <summary>
    /// Generates automated matches for a given stage.
    /// </summary>
    /// <remarks>
    /// This endpoint creates a set of matches automatically based on the provided stage identifier.
    /// The generated matches are mapped to detailed match response DTOs and returned to the client.
    /// </remarks>
    /// <param name="id">The unique identifier of the stage for which matches will be generated.</param>
    /// <returns>
    /// Returns a list of DetailedMatchResponse objects representing the generated matches.
    /// </returns>
    [HttpPost("generate/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<DetailedMatchResponse>))]
    public async Task<ActionResult> GenerateMatches(Guid id)
    {
        List<Match> response = await matchService.CreateAutomatedMatchesAsync(stageId: id);
        return Ok(mapper.Map<List<DetailedMatchResponse>>(response));
    }

    /// <summary>
    /// Retrieves a match by its id with detailed values.
    /// </summary>
    /// <param name="id">The id of the match.</param>
    /// <returns>The match response DTO.</returns>
    [AllowAnonymous]
    [HttpGet("{id:guid}/detail")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DetailedMatchResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DetailedMatchResponse>> GetMatchByIdWithScorers(Guid id)
    {
        Match? match = await matchService.GetMatchByIdWithScorersAsync(id);

        if (match is null)
        {
            return this.NotFoundProblem(nameof(Match), id);
        }

        DetailedMatchResponse matchResponse = mapper.Map<DetailedMatchResponse>(match);
        return Ok(matchResponse);
    }

    /// <summary>
    /// Retrieves a match by its id or its public slug.
    /// </summary>
    /// <param name="idOrSlug">Match identifier (GUID) or slug.</param>
    /// <returns>The match response DTO.</returns>
    [AllowAnonymous]
    [HttpGet("{idOrSlug}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DetailedMatchResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DetailedMatchResponse>> GetMatchById(string idOrSlug)
    {
        Match? match = await matchService.GetMatchByIdOrSlugAsync(idOrSlug);

        if (match is null)
        {
            return this.NotFoundProblem(nameof(Match), idOrSlug);
        }

        DetailedMatchResponse matchResponse = mapper.Map<DetailedMatchResponse>(match);
        return Ok(matchResponse);
    }

    /// <summary>
    /// Updates the date of a match.
    /// </summary>
    /// <param name="id">The id of the match to update.</param>
    /// <param name="updateRequest">The request containing the new match date.</param>
    /// <returns>Returns the result of the date update operation.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DetailedMatchResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateMatchDate(Guid id, UpdateMatchRequest updateRequest)
    {
        Match? existingMatch = await matchService.GetMatchByIdAsync(id);

        if (existingMatch is null)
        {
            return this.NotFoundProblem(nameof(Match), id);
        }

        if (existingMatch.IsFinished || existingMatch.MatchDate <= DateTime.Now)
        {
            return BadRequest(ErrorMessages.Match.CannotUpdateStartedOrFinished);
        }


        List<Guid> teamsId = [];

        if (existingMatch.HomeTeamId.HasValue)
        {
            teamsId.Add(existingMatch.HomeTeamId.Value);
        }

        if (existingMatch.VisitorTeamId.HasValue)
        {
            teamsId.Add(existingMatch.VisitorTeamId.Value);
        }

        bool canUpdate = await stageTeamMatchService.AllTeamsAssignedToStage(stageId: existingMatch.StageId, TeamIds: [.. teamsId.Distinct()]);

        if (!canUpdate)
        {
            return BadRequest(ErrorMessages.Match.TeamsNotAssignedToStage);
        }

        mapper.Map(updateRequest, existingMatch);
        await matchService.UpdateMatchAsync(existingMatch);
        DetailedMatchResponse detailedMatch = mapper.Map<DetailedMatchResponse>(existingMatch);
        return Ok(detailedMatch);
    }

    /// <summary>
    /// Deletes a match by its id.
    /// </summary>
    /// <param name="id">The id of the match to delete.</param>
    /// <returns>Returns the result of the delete operation.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> DeleteMatchById(Guid id)
    {

        await matchService.DeleteMatchAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Retrieves filtered matches with pagination.
    /// </summary>
    /// <param name="filterRequest">The filtering and pagination parameters.</param>
    /// <returns>A paginated response containing the filtered matches.</returns>
    [AllowAnonymous]
    [HttpGet()]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<DetailedMatchResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<DetailedMatchResponse>>> GetFilteredMatches([FromQuery] GetMatchesFilteredRequest filterRequest)
    {
        PaginatedResponse<Match> paginatedMatches = await matchService.GetAllMatchesAsync(filterRequest);

        PaginatedResponse<DetailedMatchResponse> response = mapper.Map<PaginatedResponse<DetailedMatchResponse>>(paginatedMatches);

        return Ok(response);
    }

    /// <summary>
    /// Updates the score of a match.
    /// </summary>
    /// <param name="id">The id of the match to update.</param>
    /// <param name="scoreRequest">The request with updated scores.</param>
    /// <returns>Returns the result of the score update operation.</returns>
    [HttpPut("{id:guid}/score")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DetailedMatchResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateMatchScore(Guid id, UpdateMatchScoreRequest scoreRequest)
    {
        Match? existingMatch = await matchService.GetMatchByIdAsync(id);
        if (existingMatch is null)
        {
            return this.NotFoundProblem(nameof(Match), id);
        }

        mapper.Map(scoreRequest, existingMatch);

        await matchService.UpdateMatchAsync(existingMatch);

        if (existingMatch.SeriesId.HasValue)
        {
            await matchSeriesService.RecalculateSeriesWinnerAsync(existingMatch.SeriesId.Value);
        }

        DetailedMatchResponse detailedMatch = mapper.Map<DetailedMatchResponse>(existingMatch);
        return Ok(detailedMatch);
    }
}
