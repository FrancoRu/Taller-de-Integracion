using Application.DTOs.Abstract.Response;
using Application.DTOs.Scorer.Request;
using Application.DTOs.Scorer.Response;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services;

public class ScorerService(IScorerRepository scorerRepository) : IScorerService
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
}
