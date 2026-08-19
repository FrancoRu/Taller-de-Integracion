using Application.DTOs.Scorer.Request;
using Application.DTOs.Scorer.Response;

using Domain.Entities.Models;

using System.Threading.Tasks;

namespace Application.Interfaces.Repositories;

public interface IScorerRepository : IGenericRepository<Scorer>
{
    Task<(System.Collections.Generic.IEnumerable<ScorerByPlayerResponse> Items, int TotalCount)> GetPlayerScoresAsync(GetScorerFilteredRequest filter);
}
