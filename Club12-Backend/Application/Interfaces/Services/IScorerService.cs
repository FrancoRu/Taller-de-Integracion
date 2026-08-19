using Application.DTOs.Abstract.Response;
using Application.DTOs.Scorer.Request;
using Application.DTOs.Scorer.Response;

using System.Threading.Tasks;

namespace Application.Interfaces.Services;

public interface IScorerService
{
    Task<PaginatedResponse<ScorerByPlayerResponse>> GetAllScorersByPlayerAsync(GetScorerFilteredRequest filter);
}
