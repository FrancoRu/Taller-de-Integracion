
using Application.DTOs.Abstract.Request;

using System;

namespace Application.DTOs.Scorer.Request;

public class GetScorerFilteredRequest : PaginatedFilterRequest
{
    public Guid? TournamentId { get; set; }

    /// <summary>
    /// Scopes the ranking to one division, including every stage in it, group and playoff alike.
    /// </summary>
    public Guid? DivisionId { get; set; }

    /// <summary>
    /// Scopes the ranking to a single stage, the group phase or one playoff bracket's round.
    /// </summary>
    public Guid? StageId { get; set; }

    public Guid? MatchId { get; set; }
    public Guid? TeamId { get; set; }
    public Guid? PlayerId { get; set; }

    /// <summary>
    /// Scopes the ranking to a whole calendar-year season instead of a single tournament.
    /// </summary>
    public int? Season { get; set; }
}
