using Application.DTOs.Abstract.Request;

using System;
namespace Application.DTOs.PlayerSanction.Request;

/// <summary>
/// Represents a request to get filtered player sanctions.
/// </summary>
public class GetPlayerSanctionsFilteredRequest : PaginatedFilterRequest
{
    /// <summary>
    /// Overrides the base PaginatedFilterRequest default to sort newest-issued-first.
    /// </summary>
    public GetPlayerSanctionsFilteredRequest()
    {
        // String literal (not nameof) because the enclosing namespace
        // "Application.DTOs.PlayerSanction" shadows the entity type, and
        // QueryableExtensions.SortBy resolves this name against the entity by
        // reflection anyway. Matches PlayerSanction.IssuedDate.
        OrderBy = "IssuedDate";
        Order = SortOrder.Descending;
    }

    public Guid? PlayerId { get; set; }

    public Guid? MatchId { get; set; }

    public Guid? TournamentId { get; set; }
    public Guid? DivisionId { get; set; }
    public Guid? StageId { get; set; }
    public Guid? TeamId { get; set; }

    public string? Description { get; set; }

    public DateTime? IssuedDate { get; set; }

    public int? Duration { get; set; }
}
