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

    Task<Match?> GetMatchByIdWithScorersAsync(Guid matchId);

    Task UpdateMatchAsync(Match matchEntity);

    Task DeleteMatchAsync(Guid id);

    Task<PaginatedResponse<Match>> GetAllMatchesAsync(GetMatchesFilteredRequest filter);

    Task GenerateFixtureAsync(Guid divisionId, IEnumerable<Team> teams);

    Task<List<Match>> CreateAutomatedMatchesAsync(Guid stageId);
}
