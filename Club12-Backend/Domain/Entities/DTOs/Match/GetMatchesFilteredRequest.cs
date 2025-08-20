using Entities.DTOs.Abstract;

namespace Entities.DTOs.Match;

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

    /// <summary>
    /// The match type (e.g., regular, playoff) to filter by.
    /// </summary>
    public Models.Matches.MatchType? Type { get; set; }

    /// <summary>
    /// Whether to filter only finished matches.
    /// </summary>
    public bool? IsFinished { get; set; }
}
