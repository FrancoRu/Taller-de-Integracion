using AutoMapper;

using Entities.DTOs.Abstract;
using Entities.DTOs.Match;
using Entities.Models.MatchEntity;
using Entities.Models.PlayerStatisticEntity;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Services.Services.MatchService;

namespace Club12.API.Controllers;

/// <summary>
/// Controller for managing Matches.
/// </summary>
/// <param name="_matchService">The Match service.</param>
/// <param name="_mapper">The AutoMapper instance.</param>
[Authorize(Roles = "SuperAdmin")]
[Route("api/matches/")]
[ApiController]
public class MatchController(IMatchService _matchService, IMapper _mapper) : ControllerBase
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
        Match mappedMatch = _mapper.Map<Match>(matchRequest);
        Match createdMatch = await _matchService.CreateMatchAsync(mappedMatch);
        MinimalMatchResponse matchResponse = _mapper.Map<MinimalMatchResponse>(createdMatch);

        return new ObjectResult(matchResponse) { StatusCode = StatusCodes.Status201Created };
    }

    /// <summary>
    /// Retrieves a match by its id with detailed values.
    /// </summary>
    /// <param name="id">The id of the match.</param>
    /// <returns>The match response DTO.</returns>
    [AllowAnonymous]
    [HttpGet("{id:guid}/detail")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DetailedMatchResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DetailedMatchResponse>> GetMatchByIdWithScorers(Guid id)
    {
        Match? match = await _matchService.GetMatchByIdWithScorersAsync(id);

        if (match is null)
        {
            return BadRequest($"Match with id {id} not found.");
        }

        DetailedMatchResponse matchResponse = _mapper.Map<DetailedMatchResponse>(match);
        return Ok(matchResponse);
    }

    /// <summary>
    /// Retrieves a match by its id.
    /// </summary>
    /// <param name="id">The id of the match.</param>
    /// <returns>The match response DTO.</returns>
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DetailedMatchResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DetailedMatchResponse>> GetMatchById(Guid id)
    {
        Match? match = await _matchService.GetMatchByIdAsync(id);

        if (match is null)
        {
            return BadRequest($"Match with id {id} not found.");
        }

        DetailedMatchResponse matchResponse = _mapper.Map<DetailedMatchResponse>(match);
        return Ok(matchResponse);
    }

    /// <summary>
    /// Updates the score of a match.
    /// </summary>
    /// <param name="id">The id of the match to update.</param>
    /// <param name="scoreRequest">The request with updated scores.</param>
    /// <returns>Returns the result of the score update operation.</returns>
    [HttpPut("{id:guid}/score")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateMatchScore(Guid id, UpdateMatchScoreRequest scoreRequest)
    {
        Match? existingMatch = await _matchService.GetMatchByIdAsync(id);
        if (existingMatch is null)
        {
            return BadRequest($"Match with ID {id} not found.");
        }

        int homeTeamTotalScore = scoreRequest.HomeTeamPlayerScores.Sum(s => s.Points);
        int visitorTeamTotalScore = scoreRequest.VisitorTeamPlayerScores.Sum(s => s.Points);

        if (homeTeamTotalScore != scoreRequest.HomeScore || visitorTeamTotalScore != scoreRequest.VisitorScore)
        {
            return BadRequest("Player points must sum up to the team score.");
        }

        _mapper.Map(scoreRequest, existingMatch);

        scoreRequest.HomeTeamPlayerScores
            .Concat(scoreRequest.VisitorTeamPlayerScores)
            .Select(playerScore => new PlayerStatistic
            {
                PlayerId = playerScore.PlayerId,
                Value = playerScore.Points,
                MatchId = id,
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow
            })
            .ToList()
            .ForEach(existingMatch.PlayerStatistics.Add);

        bool updateResult = await _matchService.UpdateMatchAsync(existingMatch);
        return !updateResult ? BadRequest("Failed to update the match score.") : NoContent();
    }

    /// <summary>
    /// Updates the date of a match.
    /// </summary>
    /// <param name="id">The id of the match to update.</param>
    /// <param name="updateRequest">The request containing the new match date.</param>
    /// <returns>Returns the result of the date update operation.</returns>
    [HttpPut("{id:guid}/date")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateMatchDate(Guid id, UpdateMatchRequest updateRequest)
    {
        Match? existingMatch = await _matchService.GetMatchByIdAsync(id);

        if (existingMatch is null)
        {
            return BadRequest($"Match with id {id} not found.");
        }

        _mapper.Map(updateRequest, existingMatch);
        bool updateResult = await _matchService.UpdateMatchAsync(existingMatch);

        return !updateResult ? BadRequest("Failed to update the match date.") : NoContent();
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
        Match? match = await _matchService.GetMatchByIdAsync(id);

        if (match is null)
        {
            return BadRequest($"Match with id {id} not found.");
        }

        bool deleteResult = await _matchService.DeleteMatchAsync(match);
        return deleteResult ? BadRequest("Could not delete match.") : NoContent();
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
        PaginatedResponse<Match> paginatedMatches = await _matchService.GetAllMatchesAsync(filterRequest);

        PaginatedResponse<DetailedMatchResponse> response = _mapper.Map<PaginatedResponse<DetailedMatchResponse>>(paginatedMatches);

        return Ok(response);
    }
}
