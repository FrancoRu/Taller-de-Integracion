using API.Utils;

using Application.DTOs.Abstract.Response;
using Application.DTOs.Match.Request;
using Application.DTOs.Match.Response;
using Application.DTOs.PlayerStatistic.Request;
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
/// Owner or Admin.
/// </summary>
/// <param name="matchService">The Match service.</param>
/// <param name="stageTeamMatchService">The stage-team match service.</param>
/// <param name="matchSeriesService">The playoff series service.</param>
/// <param name="playerStatisticService">The player-statistic service.</param>
/// <param name="stageService">The stage service (bracket-round advancement).</param>
/// <param name="mapper">The AutoMapper instance.</param>
[Route("api/matches/")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
public class MatchController(
    IMatchService matchService,
    IStageTeamMatchService stageTeamMatchService,
    IMatchSeriesService matchSeriesService,
    IPlayerStatisticService playerStatisticService,
    IStageService stageService,
    IMapper mapper) : ControllerBase
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

        // A court cannot host two matches less than 2 hours apart.
        if (existingMatch.VenueId.HasValue
            && await matchService.HasVenueScheduleConflictAsync(
                existingMatch.VenueId.Value, existingMatch.MatchDate, existingMatch.Id))
        {
            return BadRequest(ErrorMessages.Match.VenueScheduleConflict);
        }

        await matchService.UpdateMatchAsync(existingMatch);
        DetailedMatchResponse detailedMatch = mapper.Map<DetailedMatchResponse>(existingMatch);
        return Ok(detailedMatch);
    }

    /// <summary>
    /// Retrieves a stage's matches grouped by matchday (jornada, HU-63) so the
    /// fixture can be rendered as "Fecha 1 / Partido 1..2, Fecha 2 / …". The
    /// grouping key is the round, not the calendar date.
    /// </summary>
    /// <param name="stageId">The id of the stage whose fixture is requested.</param>
    /// <returns>The matches grouped and ordered by round.</returns>
    [AllowAnonymous]
    [HttpGet("stage/{stageId:guid}/by-round")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<RoundMatchesResponse>))]
    public async Task<ActionResult<List<RoundMatchesResponse>>> GetStageMatchesByRound(Guid stageId)
    {
        List<Match> matches = await matchService.GetStageMatchesByRoundAsync(stageId);

        List<RoundMatchesResponse> rounds = [.. matches
            .GroupBy(match => match.Round)
            .OrderBy(group => group.Key ?? int.MaxValue)
            .Select(group => new RoundMatchesResponse
            {
                Round = group.Key,
                Matches = mapper.Map<List<DetailedMatchResponse>>(group.ToList()),
            })];

        return Ok(rounds);
    }

    /// <summary>
    /// Reprograms/suspends a match (HU-68): marks it suspended and optionally
    /// moves it to a new date, without changing its round (HU-67) or the rest
    /// of the fixture.
    /// </summary>
    /// <param name="id">The id of the match to suspend/reprogram.</param>
    /// <param name="suspendRequest">The request with an optional new date.</param>
    /// <returns>The updated match response.</returns>
    [HttpPut("{id:guid}/suspend")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DetailedMatchResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SuspendMatch(Guid id, SuspendMatchRequest suspendRequest)
    {
        Match? updatedMatch = await matchService.SuspendMatchAsync(id, suspendRequest.MatchDate);

        if (updatedMatch is null)
        {
            return this.NotFoundProblem(nameof(Match), id);
        }

        DetailedMatchResponse detailedMatch = mapper.Map<DetailedMatchResponse>(updatedMatch);
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
        Match? updatedMatch = await matchService.LoadMatchResultAsync(id, scoreRequest.HomeScore, scoreRequest.VisitorScore);
        if (updatedMatch is null)
        {
            return this.NotFoundProblem(nameof(Match), id);
        }

        if (updatedMatch.SeriesId.HasValue)
        {
            await matchSeriesService.RecalculateSeriesWinnerAsync(updatedMatch.SeriesId.Value);
        }

        // Push a newly-decided bracket slot's winner into the next round of
        // its cup — a no-op for group-stage matches, a mid-series game that
        // did not decide the series, or the Final. Runs after the series
        // recalculation above so it sees the up-to-date decision.
        await stageService.TryAdvanceStageWinnerAsync(updatedMatch.StageId);

        DetailedMatchResponse detailedMatch = mapper.Map<DetailedMatchResponse>(updatedMatch);
        return Ok(detailedMatch);
    }

    /// <summary>
    /// Finishes a match by loading both teams' scoring sheets in one
    /// coherent operation (HU-72): the final score is DERIVED as the sum of
    /// each team's listed player points, instead of being typed in separately
    /// and checked against a sheet afterward (the older <c>match-sheet</c>
    /// flow, still available for corrections one team at a time).
    /// </summary>
    /// <param name="id">The id of the match to finish.</param>
    /// <param name="request">Both teams' per-player points.</param>
    /// <returns>The finalized match.</returns>
    [HttpPut("{id:guid}/result-from-sheets")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DetailedMatchResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> LoadMatchResultFromSheets(Guid id, LoadMatchResultFromSheetsRequest request)
    {
        request.MatchId = id;
        Match? updatedMatch = await playerStatisticService.LoadMatchResultFromSheetsAsync(request);
        if (updatedMatch is null)
        {
            return this.NotFoundProblem(nameof(Match), id);
        }

        if (updatedMatch.SeriesId.HasValue)
        {
            await matchSeriesService.RecalculateSeriesWinnerAsync(updatedMatch.SeriesId.Value);
        }

        // Push a newly-decided bracket slot's winner into the next round of
        // its cup — a no-op for group-stage matches, a mid-series game that
        // did not decide the series, or the Final. Runs after the series
        // recalculation above so it sees the up-to-date decision.
        await stageService.TryAdvanceStageWinnerAsync(updatedMatch.StageId);

        DetailedMatchResponse detailedMatch = mapper.Map<DetailedMatchResponse>(updatedMatch);
        return Ok(detailedMatch);
    }

    /// <summary>
    /// Marks a match as a walkover (HU-73), awarding the regulation default
    /// result to the present team.
    /// </summary>
    /// <param name="id">The id of the match to mark as a walkover.</param>
    /// <param name="walkOverRequest">The request identifying the present team.</param>
    /// <returns>The updated match response.</returns>
    [HttpPut("{id:guid}/walkover")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DetailedMatchResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> LoadWalkOver(Guid id, LoadWalkOverRequest walkOverRequest)
    {
        Match? updatedMatch = await matchService.LoadWalkOverAsync(id, walkOverRequest.PresentTeamId, walkOverRequest.PresentTeamScore);
        if (updatedMatch is null)
        {
            return this.NotFoundProblem(nameof(Match), id);
        }

        if (updatedMatch.SeriesId.HasValue)
        {
            await matchSeriesService.RecalculateSeriesWinnerAsync(updatedMatch.SeriesId.Value);
        }

        // Push a newly-decided bracket slot's winner into the next round of
        // its cup — a no-op for group-stage matches, a mid-series game that
        // did not decide the series, or the Final. Runs after the series
        // recalculation above so it sees the up-to-date decision.
        await stageService.TryAdvanceStageWinnerAsync(updatedMatch.StageId);

        DetailedMatchResponse detailedMatch = mapper.Map<DetailedMatchResponse>(updatedMatch);
        return Ok(detailedMatch);
    }
}
