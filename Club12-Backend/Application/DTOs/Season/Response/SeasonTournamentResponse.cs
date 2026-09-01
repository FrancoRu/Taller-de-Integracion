using Application.DTOs.Abstract.Response;

using Domain.Enums;

namespace Application.DTOs.Season.Response;

/// <summary>
/// Lightweight view of a tournament as it appears inside a
/// <see cref="SeasonResponse"/>: just enough to list and link the tournaments
/// grouped under a season, without pulling their full division graph.
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
    /// Competitive category (gender) of the tournament (HU-48). Kept per
    /// tournament even when grouped under a season.
    /// </summary>
    public TournamentCategory Category { get; set; }

    /// <summary>
    /// The tournament's lifecycle status, so the season's tournament cards can
    /// show the same status chip every other tournament list view does.
    /// </summary>
    public TournamentStatus Status { get; set; }
}
