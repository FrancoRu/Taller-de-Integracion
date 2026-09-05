using Application.DTOs.Abstract.Response;

using Domain.Enums;

namespace Application.DTOs.Season.Response;

/// <summary>
/// Lightweight view of a tournament inside a SeasonResponse, without its full division graph.
/// </summary>
public class SeasonTournamentResponse : BaseEntityResponse
{
    /// <summary>
    /// The name of the tournament.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public tournament links.
    /// </summary>
    public required string Slug { get; set; }

    /// <summary>
    /// Competitive category of the tournament, kept per tournament even when grouped under a season.
    /// </summary>
    public TournamentCategory Category { get; set; }

    /// <summary>
    /// The tournament's lifecycle status, matching every other tournament list view's status chip.
    /// </summary>
    public TournamentStatus Status { get; set; }
}
