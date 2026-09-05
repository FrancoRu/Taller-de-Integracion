using Application.DTOs.Abstract.Response;
using Application.DTOs.Match.Request;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

public interface IMatchService
{
    /// <summary>
    /// Creates a match and generates its unique slug from the home and visitor team names and match date.
    /// </summary>
    Task<Match> CreateMatchAsync(Match matchEntity);

    Task<Match?> GetMatchByIdAsync(Guid matchId);

    /// <summary>
    /// Retrieves a match by its id or its slug, treating the value as an id when it parses as a GUID and otherwise looking it up as a slug.
    /// </summary>
    /// <param name="idOrSlug">The match's GUID id or its slug.</param>
    /// <returns>The matching match, or null if not found.</returns>
    Task<Match?> GetMatchByIdOrSlugAsync(string idOrSlug);

    Task<Match?> GetMatchByIdWithScorersAsync(Guid matchId);

    Task UpdateMatchAsync(Match matchEntity);

    /// <summary>
    /// True when another match, not excludeMatchId, is scheduled at the same venue less than 2 hours from matchDate, since two matches on one court must be at least 2 hours apart.
    /// </summary>
    Task<bool> HasVenueScheduleConflictAsync(Guid venueId, DateTime matchDate, Guid excludeMatchId);

    /// <summary>
    /// Loads a decisive final result for a match, rejecting a tied score.
    /// </summary>
    Task<Match?> LoadMatchResultAsync(Guid matchId, int homeScore, int visitorScore);

    /// <summary>
    /// Applies a walkover result to a match, awarding the regulation default to the present team.
    /// </summary>
    Task<Match?> LoadWalkOverAsync(Guid matchId, Guid presentTeamId, int? presentTeamScore);

    /// <summary>
    /// Reprograms or suspends a match, marking it MatchStatus.Suspended and optionally moving it to a new calendar date.
    /// </summary>
    Task<Match?> SuspendMatchAsync(Guid matchId, DateTime? newMatchDate);

    Task DeleteMatchAsync(Guid id);

    Task<PaginatedResponse<Match>> GetAllMatchesAsync(GetMatchesFilteredRequest filter);

    /// <summary>
    /// Retrieves every match of a stage ordered by matchday, round 1 first then round 2 and so on.
    /// </summary>
    Task<List<Match>> GetStageMatchesByRoundAsync(Guid stageId);

    Task<List<Match>> CreateAutomatedMatchesAsync(Guid stageId);
}
