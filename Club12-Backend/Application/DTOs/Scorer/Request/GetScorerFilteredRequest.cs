
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

    /// <summary>
    /// Scopes the goleadores ranking to a whole SEASON (HU-85) rather than a
    /// single tournament. A "season" is the calendar year of a tournament's
    /// <see cref="Domain.Entities.Models.Tournament.StartDate"/> — the simplest
    /// value derivable from existing data with no schema change (matches the
    /// "el mejor de 2026" wording in HU-85). When set, the ranking sums every
    /// point a person scored across all tournaments that started in that year,
    /// grouped by their stable PlayerId. Leaving <see cref="TournamentId"/> and
    /// this both unset yields the ALL-TIME ranking (points summed across every
    /// season). Season and <see cref="TournamentId"/> are independent filters.
    /// </summary>
    public int? Season { get; set; }
}
