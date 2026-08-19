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

    Task DeleteMatchAsync(Guid id);

    Task<PaginatedResponse<Match>> GetAllMatchesAsync(GetMatchesFilteredRequest filter);

    Task<List<Match>> CreateAutomatedMatchesAsync(Guid stageId);
}
