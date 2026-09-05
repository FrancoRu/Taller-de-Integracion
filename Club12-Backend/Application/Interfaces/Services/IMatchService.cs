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
    /// Creates a match and generates its unique slug from the home/visitor
    /// team names and match date.
    /// </summary>
    Task<Match> CreateMatchAsync(Match matchEntity);

    Task<Match?> GetMatchByIdAsync(Guid matchId);

    /// <summary>
    /// Retrieves a match by its id or its slug. The value is treated as an id
    /// when it parses as a GUID, otherwise it is looked up as a slug.
    /// </summary>
    /// <param name="idOrSlug">The match's GUID id or its slug.</param>
    /// <returns>The matching match, or null if not found.</returns>
    Task<Match?> GetMatchByIdOrSlugAsync(string idOrSlug);

    Task<Match?> GetMatchByIdWithScorersAsync(Guid matchId);

    Task UpdateMatchAsync(Match matchEntity);

    /// <summary>
    /// True when another match (not <paramref name="excludeMatchId"/>) is
    /// scheduled at the same venue less than 2 hours from
    /// <paramref name="matchDate"/> — two matches on one court must be at least
    /// 2 hours apart.
    /// </summary>
    Task<bool> HasVenueScheduleConflictAsync(Guid venueId, DateTime matchDate, Guid excludeMatchId);

    /// <summary>
    /// Loads a decisive final result for a match (HU-69/HU-70), rejecting a
    /// tied score. Returns the updated match, or null if it does not exist.
    /// </summary>
    Task<Match?> LoadMatchResultAsync(Guid matchId, int homeScore, int visitorScore);

    /// <summary>
    /// Applies a walkover result to a match (HU-73), awarding the regulation
    /// default to the present team. Returns the updated match, or null if it
    /// does not exist.
    /// </summary>
    Task<Match?> LoadWalkOverAsync(Guid matchId, Guid presentTeamId, int? presentTeamScore);

    /// <summary>
    /// Reprograms/suspends a match (HU-68): marks it
    /// <see cref="Domain.Enums.MatchStatus.Suspended"/> and optionally moves it
    /// to a new calendar date, without altering its <see cref="Match.Round"/>
    /// (HU-67) or the rest of the fixture. Returns the updated match, or null if
    /// it does not exist.
    /// </summary>
    Task<Match?> SuspendMatchAsync(Guid matchId, DateTime? newMatchDate);

    Task DeleteMatchAsync(Guid id);

    Task<PaginatedResponse<Match>> GetAllMatchesAsync(GetMatchesFilteredRequest filter);

    /// <summary>
    /// Retrieves every match of a stage ordered by matchday (jornada, HU-63):
    /// round 1 first, then round 2, … so the caller can render the fixture
    /// grouped by round ("Fecha 1 / Partido 1..2, Fecha 2 / …") rather than by
    /// calendar date. Matches without a round (e.g. knockout) sort last.
    /// </summary>
    Task<List<Match>> GetStageMatchesByRoundAsync(Guid stageId);

    Task<List<Match>> CreateAutomatedMatchesAsync(Guid stageId);
}
