using Application.DTOs.Abstract.Response;
using Application.DTOs.Match.Request;
using Application.DTOs.Scorer.Request;
using Application.DTOs.Scorer.Response;
using Application.Interfaces.Mappers;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;

using Domain.Entities.Models;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services;

public class ScorerService(IScorerRepository scorerRepository, IMatchService matchService, IScorerMapper mapper) : IScorerService
{
    public async Task<PaginatedResponse<ScorerByPlayerResponse>> GetAllScorersByPlayerAsync(GetScorerFilteredRequest filter)
    {
        (IEnumerable<ScorerByPlayerResponse> items, int totalCount) = await scorerRepository.GetPlayerScoresAsync(filter);

        return new()
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = items
        };
    }

    public async Task<PaginatedResponse<ScorerByTeamResponse>> GetAllScorersByTeamAsync(GetMatchesFilteredRequest filter)
    {
        PaginatedResponse<Match> matches = await matchService.GetAllMatchesAsync(filter);
        return mapper.FromPaginatedMatchToPaginatedScorerByTeamResponse(matches);
    }
}
