using Application.DTOs.Abstract.Request;
using Domain.Entities.Models;
using Domain.Enums;
using System;

namespace Application.DTOs.Match.Request;

/// <summary>
/// Represents a request to filter and paginate matches.
/// </summary>
public class GetMatchesFilteredRequest : PaginatedFilterRequest
{
    /// <summary>
    /// The name of the home team to filter by.
    /// </summary>
    public string? HomeTeamName { get; set; }

    /// <summary>
    /// The name of the visitor team to filter by.
    /// </summary>
    public string? VisitorTeamName { get; set; }

    /// <summary>
    /// The stage name to filter by.
    /// </summary>
    public Guid? StageId { get; set; }

    public Guid? DivisionId { get; set; }

    public Guid? TournamentId { get; set; }

    /// <summary>
    /// The match type (e.g., regular, playoff) to filter by.
    /// </summary>
    public MatchType? Type { get; set; }

    /// <summary>
    /// Whether to filter only finished matches.
    /// </summary>
    public bool? IsFinished { get; set; }
}
