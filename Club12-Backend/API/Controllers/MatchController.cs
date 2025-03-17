using AutoMapper;

using Entities.DTOs.Abstract;
using Entities.DTOs.Match;
using Entities.Models.MatchEntity;
using Entities.Models.PlayerStatisticEntity;
using Entities.Models.PlayoffSeriesEntity;
using Entities.Models.TeamEntity;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Services.Services.MatchService;
using Services.Services.PlayoffService;

using MatchType = Entities.Models.MatchTypeEnum.MatchType;

namespace Club12.API.Controllers;

/// <summary>
/// Controller for managing Matches.
/// </summary>
/// <param name="_matchService">The Match service.</param>
/// <param name="_playoffSeriesService"></param>
/// <param name="_mapper">The AutoMapper instance.</param>
[Authorize(Roles = "SuperAdmin")]
[Route("api/matches/")]
[ApiController]
public class MatchController(IMatchService _matchService, IPlayoffSeriesService _playoffSeriesService, IMapper _mapper) : ControllerBase
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

        bool matchUpdateSuccess = await UpdateMatchScoreAsync(existingMatch, scoreRequest);
        if (!matchUpdateSuccess)
        {
            return BadRequest("Failed to update the match score.");
        }

        if (existingMatch.Type == MatchType.Playoff && existingMatch.PlayoffSeriesId.HasValue)
        {
            bool seriesUpdateSuccess = await HandlePlayoffSeriesAsync(existingMatch);
            if (!seriesUpdateSuccess)
            {
                return BadRequest("Failed to update the playoff series.");
            }
        }

        return NoContent();
    }

    /// <summary>
    /// Updates the score of a match and player statistics.
    /// </summary>
    /// <param name="match">The match to update.</param>
    /// <param name="scoreRequest">The request with updated scores.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    private async Task<bool> UpdateMatchScoreAsync(Match match, UpdateMatchScoreRequest scoreRequest)
    {
        int homeTeamTotalScore = scoreRequest.HomeTeamPlayerScores.Sum(s => s.Points);
        int visitorTeamTotalScore = scoreRequest.VisitorTeamPlayerScores.Sum(s => s.Points);

        if (homeTeamTotalScore != scoreRequest.HomeScore || visitorTeamTotalScore != scoreRequest.VisitorScore)
        {
            return false; // Player points must sum up to the team score
        }

        _mapper.Map(scoreRequest, match);
        UpdatePlayerStatistics(match, scoreRequest);

        return await _matchService.UpdateMatchAsync(match); // Save the updated match
    }

    /// <summary>
    /// Updates player statistics for the match.
    /// </summary>
    /// <param name="match">The match to update.</param>
    /// <param name="scoreRequest">The request with updated scores.</param>
    private static void UpdatePlayerStatistics(Match match, UpdateMatchScoreRequest scoreRequest) => scoreRequest.HomeTeamPlayerScores
            .Concat(scoreRequest.VisitorTeamPlayerScores)
            .Select(playerScore => new PlayerStatistic
            {
                PlayerId = playerScore.PlayerId,
                Value = playerScore.Points,
                MatchId = match.Id,
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow
            })
            .ToList()
            .ForEach(match.PlayerStatistics.Add);

    /// <summary>
    /// Handles the playoff series logic after a match is updated.
    /// </summary>
    /// <param name="match">The match that was updated.</param>
    /// <returns>True if the playoff series was successfully updated; otherwise, false.</returns>
    private async Task<bool> HandlePlayoffSeriesAsync(Match match)
    {
        PlayoffSeries? playoffSeries = await _playoffSeriesService.GetSeriesByIdAsync(match.PlayoffSeriesId.Value);
        if (playoffSeries is null)
        {
            return false; // Playoff series not found
        }

        // Update series wins based on the match result
        if (match.HomeScore > match.VisitorScore)
        {
            playoffSeries.HomeTeamWins++;
        }
        else
        {
            playoffSeries.VisitorTeamWins++;
        }

        // Check if the series is finished
        if (playoffSeries.HomeTeamWins >= playoffSeries.GamesRequiredToWin || playoffSeries.VisitorTeamWins >= playoffSeries.GamesRequiredToWin)
        {
            playoffSeries.IsFinished = true;
            playoffSeries.WinningTeamId = playoffSeries.HomeTeamWins >= playoffSeries.GamesRequiredToWin
                ? match.HomeTeamId
                : match.VisitorTeamId;

            // Mark remaining matches in the series as finished (if any)
            foreach (Match? remainingMatch in playoffSeries.Matches.Where(m => !m.IsFinished))
            {
                remainingMatch.IsFinished = true;
                await _matchService.UpdateMatchAsync(remainingMatch);
            }

            // If there's a next series, assign the winning team based on seed
            if (playoffSeries.NextSeriesId.HasValue)
            {
                PlayoffSeries? nextSeries = await _playoffSeriesService.GetSeriesByIdAsync(playoffSeries.NextSeriesId.Value);
                if (nextSeries is null)
                {
                    return false; // Next series not found
                }

                // Get the winning team from the current match
                Team? winningTeam = playoffSeries.WinningTeamId == match.HomeTeamId
                    ? match.HomeTeam
                    : match.VisitorTeam;

                if (winningTeam is null)
                {
                    return false; // Winning team not found in the match
                }

                // Find the next match in the next series using GameNumber
                Match? nextMatch = nextSeries.Matches.FirstOrDefault(m => m.GameNumber == match.GameNumber);
                if (nextMatch is null)
                {
                    return false; // Next match not found in the next series
                }


                Team? otherTeam = nextMatch.HomeTeam ?? nextMatch.VisitorTeam;

                if (otherTeam is null)
                {
                    if (nextMatch.HomeTeam is null)
                    {
                        nextMatch.HomeTeam = winningTeam;
                    }
                    else
                    {
                        nextMatch.VisitorTeam = winningTeam;
                    }
                }
                else
                {
                    Team homeTeam = winningTeam.Seed < otherTeam.Seed ? winningTeam : otherTeam;
                    Team visitorTeam = winningTeam.Seed < otherTeam.Seed ? otherTeam : winningTeam;

                    nextMatch.HomeTeam = homeTeam;
                    nextMatch.VisitorTeam = visitorTeam;
                }


                bool matchUpdated = await _matchService.UpdateMatchAsync(nextMatch);
                if (!matchUpdated)
                {
                    return false;
                }

                bool seriesUpdated = await _playoffSeriesService.UpdateSeriesAsync(nextSeries);
                if (!seriesUpdated)
                {
                    return false;
                }
            }
        }

        return await _playoffSeriesService.UpdateSeriesAsync(playoffSeries);
    }
}
