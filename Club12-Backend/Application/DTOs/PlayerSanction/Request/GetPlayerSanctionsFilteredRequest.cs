using Application.DTOs.Abstract.Request;

using System;
namespace Application.DTOs.PlayerSanction.Request;

/// <summary>
/// Represents a request to get filtered player sanctions.
/// </summary>
public class GetPlayerSanctionsFilteredRequest : PaginatedFilterRequest
{
    /// <summary>
    /// Defaults the list to newest-issued-first. The admin and public
    /// sanctions pages call <c>GET /api/player-sanctions/find</c> without a
    /// sort parameter and never wire server-side column sorting, so this is
    /// the effective order of the table. Overrides the base
    /// <see cref="PaginatedFilterRequest"/> default of
    /// <c>DateCreated</c> ascending; an explicit <c>orderBy</c>/<c>order</c>
    /// in the query string still wins.
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

    /// <summary>
    /// The unique identifier of the player to filter sanctions by.
    /// </summary>
    public Guid? PlayerId { get; set; }

    /// <summary>
    /// The unique identifier of the match to filter sanctions by.
    /// </summary>
    public Guid? MatchId { get; set; }

    public Guid? TournamentId { get; set; }
    public Guid? DivisionId { get; set; }
    public Guid? StageId { get; set; }
    public Guid? TeamId { get; set; }
    /// <summary>
    /// The description text to filter sanctions by.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The date the sanction was issued, used to filter sanctions.
    /// </summary>
    public DateTime? IssuedDate { get; set; }

    /// <summary>
    /// The duration of the sanction, used to filter sanctions.
    /// </summary>
    public int? Duration { get; set; }
}
