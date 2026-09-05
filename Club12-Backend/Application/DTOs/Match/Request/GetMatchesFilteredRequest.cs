using Application.DTOs.Abstract.Request;

using Domain.Enums;

using System;

namespace Application.DTOs.Match.Request;

/// <summary>
/// Represents a request to filter and paginate matches.
/// </summary>
public class GetMatchesFilteredRequest : PaginatedFilterRequest
{
    public string? HomeTeamName { get; set; }

    public string? VisitorTeamName { get; set; }

    public Guid? StageId { get; set; }

    public Guid? DivisionId { get; set; }

    public Guid? TournamentId { get; set; }

    public MatchType? Type { get; set; }

    public bool? IsFinished { get; set; }
}
