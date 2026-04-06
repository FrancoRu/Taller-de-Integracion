
using Application.DTOs.Abstract.Request;
using System;

namespace Application.DTOs.Scorer.Request;

public class GetScorerFilteredRequest: PaginatedFilterRequest
{
    public Guid? TournamentId { get; set; }

    public Guid? MatchId { get; set; }
    public Guid? TeamId { get; set; }
    public Guid? PlayerId { get; set; }
}
