using Application.DTOs.Abstract.Response;
using Application.DTOs.Match.Request;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Represents a service for managing matches.
/// </summary>
public interface IMatchService
{
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

    Task DeleteMatchAsync(Guid id);

    Task<PaginatedResponse<Match>> GetAllMatchesAsync(GetMatchesFilteredRequest filter);

    Task<List<Match>> CreateAutomatedMatchesAsync(Guid stageId);
}
