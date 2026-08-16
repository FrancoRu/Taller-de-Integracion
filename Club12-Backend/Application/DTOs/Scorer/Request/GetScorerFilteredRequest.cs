
using Application.DTOs.Abstract.Request;

using System;

namespace Application.DTOs.Scorer.Request;

public class GetScorerFilteredRequest : PaginatedFilterRequest
{
    public Guid? TournamentId { get; set; }

    /// <summary>
    /// Scopes the ranking to one division (a zone or the cross-division
    /// cup) — every stage in it, group and playoff alike.
    /// </summary>
    public Guid? DivisionId { get; set; }

    /// <summary>
    /// Scopes the ranking to a single stage (e.g. just the group phase, or
    /// just one named playoff bracket's round).
    /// </summary>
    public Guid? StageId { get; set; }

    public Guid? MatchId { get; set; }
    public Guid? TeamId { get; set; }
    public Guid? PlayerId { get; set; }
}
