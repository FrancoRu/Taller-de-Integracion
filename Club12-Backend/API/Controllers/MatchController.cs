using AutoMapper;
using Application.DTOs.Abstract.Response;
using Application.DTOs.Match.Request;
using Application.DTOs.Match.Response;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities.Models;

namespace API.Controllers;

/// <summary>
/// Controller for managing Matches.
/// </summary>
/// <param name="_matchService">The Match service.</param>
/// <param name="_mapper">The AutoMapper instance.</param>
//[Authorize(Roles = "SuperAdmin")]
[Route("api/matches/")]
[ApiController]
public class MatchController(IMatchService _matchService, IStageTeamMatchService _stageTeamMatchService ,IMapper _mapper) : ControllerBase
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

    [HttpPost("generate/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<DetailedMatchResponse>))]
    public async Task<ActionResult> GenerateMatches(Guid id)
    {
        List<Match> response = await _matchService.CreateAutomatedMatchesAsync(stageId: id);
        return Ok(_mapper.Map<List<DetailedMatchResponse>>(response));
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
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(DetailedMatchResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateMatchDate(Guid id, UpdateMatchRequest updateRequest)
    {
        Match? existingMatch = await _matchService.GetMatchByIdAsync(id);

        if (existingMatch is null)
        {
            return BadRequest($"Match with id {id} not found.");
        }

        if (existingMatch.IsFinished || existingMatch.MatchDate <= DateTime.Now)
        {
            return BadRequest("Cannot update a match that has already started or finished.");
        }


        List<Guid> teamsId = [];

        if(existingMatch.HomeTeamId.HasValue)
        {
            teamsId.Add(existingMatch.HomeTeamId.Value);
        }

        if(existingMatch.VisitorTeamId.HasValue )
        {
            teamsId.Add(existingMatch.VisitorTeamId.Value);
        }

        bool canUpdate = await _stageTeamMatchService.AllTeamsAssignedToStage(stageId: existingMatch.StageId, TeamIds: [.. teamsId.Distinct()]);

        if (!canUpdate)
        {
            return BadRequest("Cannot update match date because one or both teams are not assigned to the stage.");
        }

        _mapper.Map(updateRequest, existingMatch);
        await _matchService.UpdateMatchAsync(existingMatch);
        DetailedMatchResponse detailedMatch = _mapper.Map<DetailedMatchResponse>(existingMatch);
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
        
        await _matchService.DeleteMatchAsync(id);
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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DetailedMatchResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateMatchScore(Guid id, UpdateMatchScoreRequest scoreRequest)
    {
        Match? existingMatch = await _matchService.GetMatchByIdAsync(id);
        if (existingMatch is null)
        {
            return BadRequest($"Match with ID {id} not found.");
        }

        _mapper.Map(scoreRequest, existingMatch);

        await _matchService.UpdateMatchAsync(existingMatch);

        DetailedMatchResponse detailedMatch = _mapper.Map<DetailedMatchResponse>(existingMatch);
        return Ok(detailedMatch);
    }

    ///// <summary>
    ///// Updates the score of a match and player statistics.
    ///// </summary>
    ///// <param name="match">The match to update.</param>
    ///// <param name="scoreRequest">The request with updated scores.</param>
    ///// <returns>True if the update was successful; otherwise, false.</returns>
    //private async Task<bool> UpdateMatchScoreAsync(Match match, UpdateMatchScoreRequest scoreRequest)
    //{
    //    int homeTeamTotalScore = scoreRequest.HomeTeamPlayerScores.Sum(s => s.Points);
    //    int visitorTeamTotalScore = scoreRequest.VisitorTeamPlayerScores.Sum(s => s.Points);

    //    if (homeTeamTotalScore != scoreRequest.HomeScore || visitorTeamTotalScore != scoreRequest.VisitorScore)
    //    {
    //        return false; // Player points must sum up to the team score
    //    }

    //    _mapper.Map(scoreRequest, match);
    //    UpdatePlayerStatistics(match, scoreRequest);

    //    return await _matchService.UpdateMatchAsync(match); // Save the updated match
    //}

    /// <summary>
    /// Updates player statistics for the match.
    /// </summary>
    /// <param name="match">The match to update.</param>
    /// <param name="scoreRequest">The request with updated scores.</param>
    //private static void UpdatePlayerStatistics(Match match, UpdateMatchScoreRequest scoreRequest) => scoreRequest.HomeTeamPlayerScores
    //        .Concat(scoreRequest.VisitorTeamPlayerScores)
    //        .Select(playerScore => new PlayerStatistic
    //        {
    //            PlayerId = playerScore.PlayerId,
    //            Value = playerScore.Points,
    //            MatchId = match.Id,
    //            DateCreated = DateTime.Now,
    //            DateUpdated = DateTime.Now
    //        })
    //        .ToList()
    //        .ForEach(match.PlayerStatistics.Add);

    /// <summary>
    /// Handles the playoff series logic after a match is updated.
    /// </summary>
    /// <param name="match">The match that was updated.</param>
    /// <returns>True if the playoff series was successfully updated; otherwise, false.</returns>
    //private async Task<bool> HandlePlayoffSeriesAsync(Match match)
    //{
    //    PlayoffSerie? playoffSeries = await _playoffSeriesService.GetSeriesByIdAsync(match.PlayoffSeriesId.Value);
    //    if (playoffSeries is null)
    //    {
    //        return false; // Playoff series not found
    //    }

    //    // Update series wins based on the match result
    //    if (match.HomeScore > match.VisitorScore)
    //    {
    //        playoffSeries.HomeTeamWins++;
    //    }
    //    else
    //    {
    //        playoffSeries.VisitorTeamWins++;
    //    }

    //    // Check if the series is finished
    //    if (playoffSeries.HomeTeamWins >= playoffSeries.GamesRequiredToWin || playoffSeries.VisitorTeamWins >= playoffSeries.GamesRequiredToWin)
    //    {
    //        playoffSeries.IsFinished = true;
    //        playoffSeries.WinningTeamId = playoffSeries.HomeTeamWins >= playoffSeries.GamesRequiredToWin
    //            ? match.HomeTeamId
    //            : match.VisitorTeamId;

    //        // Mark remaining matches in the series as finished (if any)
    //        foreach (Match? remainingMatch in playoffSeries.Matches.Where(m => !m.IsFinished))
    //        {
    //            remainingMatch.IsFinished = true;
    //            await _matchService.UpdateMatchAsync(remainingMatch);
    //        }

    //        // If there's a next series, assign the winning team based on seed
    //        if (playoffSeries.NextSeriesId.HasValue)
    //        {
    //            PlayoffSerie? nextSeries = await _playoffSeriesService.GetSeriesByIdAsync(playoffSeries.NextSeriesId.Value);
    //            if (nextSeries is null)
    //            {
    //                return false; // Next series not found
    //            }

    //            // Get the winning team from the current match
    //            Team? winningTeam = playoffSeries.WinningTeamId == match.HomeTeamId
    //                ? match.HomeTeam
    //                : match.VisitorTeam;

    //            if (winningTeam is null)
    //            {
    //                return false; // Winning team not found in the match
    //            }

    //            // Find the next match in the next series using GameNumber
    //            Match? nextMatch = nextSeries.Matches.FirstOrDefault(m => m.GameNumber == match.GameNumber);
    //            if (nextMatch is null)
    //            {
    //                return false; // Next match not found in the next series
    //            }


    //            Team? otherTeam = nextMatch.HomeTeam ?? nextMatch.VisitorTeam;

    //            if (otherTeam is null)
    //            {
    //                if (nextMatch.HomeTeam is null)
    //                {
    //                    nextMatch.HomeTeam = winningTeam;
    //                }
    //                else
    //                {
    //                    nextMatch.VisitorTeam = winningTeam;
    //                }
    //            }
    //            else
    //            {
    //                Team homeTeam = winningTeam.Seed < otherTeam.Seed ? winningTeam : otherTeam;
    //                Team visitorTeam = winningTeam.Seed < otherTeam.Seed ? otherTeam : winningTeam;

    //                nextMatch.HomeTeam = homeTeam;
    //                nextMatch.VisitorTeam = visitorTeam;
    //            }


    //            bool matchUpdated = await _matchService.UpdateMatchAsync(nextMatch);
    //            if (!matchUpdated)
    //            {
    //                return false;
    //            }

    //            bool seriesUpdated = await _playoffSeriesService.UpdateSeriesAsync(nextSeries);
    //            if (!seriesUpdated)
    //            {
    //                return false;
    //            }
    //        }
    //    }

    //    return await _playoffSeriesService.UpdateSeriesAsync(playoffSeries);
    //}
}
