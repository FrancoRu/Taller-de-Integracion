using Application.DTOs.Abstract.Response;
using Application.DTOs.Scorer.Response;

using Domain.Entities.Models;

namespace Application.Interfaces.Mappers;

public interface IScorerMapper
{
    ScorerByPlayerResponse FromScorerToScorerByPlayerResponse(Scorer scorer);

    ScorerByTeamResponse FromScorerToScorerByTeamResponse(Scorer scorer);

    PaginatedResponse<ScorerByPlayerResponse> FromPaginatedScorerToPaginatedScorerByPlayerResponse(PaginatedResponse<Scorer> paginatedScorers);

    PaginatedResponse<ScorerByTeamResponse> FromPaginatedMatchToPaginatedScorerByTeamResponse(PaginatedResponse<Match> paginatedMatches);
}
