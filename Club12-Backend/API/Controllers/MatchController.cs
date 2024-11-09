using AutoMapper;

using Entities.DTOs.Abstract;
using Entities.DTOs.Match;
using Entities.Models.MatchEntity;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Services.Services.MatchService;

namespace Club12.API.Controllers;

/// <summary>
/// Controller for managing Matches.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MatchController"/> class.
/// </remarks>
/// <param name="matchService">The Match service.</param>
/// <param name="mapper">The AutoMapper instance.</param>
[Authorize(Roles = "SuperAdmin")]
[Route("api/matches/")]
[ApiController]
public class MatchController(IMatchService matchService, IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Creates a new match.
    /// </summary>
    /// <param name="matchRequest">The match request DTO.</param>
    /// <returns>The created match response.</returns>
    [HttpPost()]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(MatchResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<MatchResponse> CreateMatch(CreateMatchRequest matchRequest)
    {
        Match mappedMatch = mapper.Map<Match>(matchRequest);
        Match createdMatch = matchService.CreateMatch(mappedMatch);
        MatchResponse matchResponse = mapper.Map<MatchResponse>(createdMatch);

        return CreatedAtAction(nameof(GetMatchById), new { id = matchResponse.Id }, matchResponse);
    }

    /// <summary>
    /// Retrieves a match by its id.
    /// </summary>
    /// <param name="id">The id of the match.</param>
    /// <returns>The match response DTO.</returns>
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MatchResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<MatchResponse> GetMatchById(Guid id)
    {
        Match? match = matchService.GetMatchById(id);

        if (match is null)
        {
            return BadRequest($"Match with id {id} not found.");
        }

        MatchResponse matchResponse = mapper.Map<MatchResponse>(match);
        return Ok(matchResponse);
    }

    /// <summary>
    /// Updates the score of a match.
    /// </summary>
    /// <param name="matchId">The id of the match to update.</param>
    /// <param name="scoreRequest">The request with updated scores.</param>
    /// <returns>Returns the result of the score update operation.</returns>
    [HttpPut("{matchId:guid}/score")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateMatchScore(Guid matchId, UpdateMatchScoreRequest scoreRequest)
    {
        Match? existingMatch = matchService.GetMatchById(matchId);

        if (existingMatch is null)
        {
            return BadRequest($"Match with id {matchId} not found.");
        }

        mapper.Map(scoreRequest, existingMatch);
        bool updateResult = await matchService.UpdateMatchAsync(existingMatch);

        return !updateResult ? BadRequest("Failed to update the match score.") : Ok();
    }

    /// <summary>
    /// Updates the date of a match.
    /// </summary>
    /// <param name="matchId">The id of the match to update.</param>
    /// <param name="updateRequest">The request containing the new match date.</param>
    /// <returns>Returns the result of the date update operation.</returns>
    [HttpPut("{matchId:guid}/date")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateMatchDate(Guid matchId, UpdateMatchRequest updateRequest)
    {
        Match? existingMatch = matchService.GetMatchById(matchId);

        if (existingMatch is null)
        {
            return BadRequest($"Match with id {matchId} not found.");
        }

        mapper.Map(updateRequest, existingMatch);
        bool updateResult = await matchService.UpdateMatchAsync(existingMatch);

        return !updateResult ? BadRequest("Failed to update the match date.") : Ok();
    }

    /// <summary>
    /// Deletes a match by its id.
    /// </summary>
    /// <param name="id">The id of the match to delete.</param>
    /// <returns>Returns the result of the delete operation.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult DeleteMatchById(Guid id)
    {
        Match? match = matchService.GetMatchById(id);

        if (match is null)
        {
            return BadRequest($"Match with id {id} not found.");
        }

        matchService.DeleteMatch(match);
        return Ok();
    }

    /// <summary>
    /// Retrieves filtered matches with pagination.
    /// </summary>
    /// <param name="filterRequest">The filtering and pagination parameters.</param>
    /// <returns>A paginated response containing the filtered matches.</returns>
    [AllowAnonymous]
    [HttpGet()]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<MatchResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<MatchResponse>>> GetFilteredMatches([FromQuery] GetMatchesFilteredRequest filterRequest)
    {
        PaginatedResponse<Match> paginatedMatches = await matchService.GetAllMatchesAsync(filterRequest);

        PaginatedResponse<MatchResponse> response = mapper.Map<PaginatedResponse<MatchResponse>>(paginatedMatches);

        return Ok(response);
    }
}
