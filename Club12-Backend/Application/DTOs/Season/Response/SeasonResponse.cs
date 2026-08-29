using Application.DTOs.Abstract.Response;

using System.Collections.Generic;

namespace Application.DTOs.Season.Response;

/// <summary>
/// Response model for returning season ("Temporada") details, including the
/// lightweight list of tournaments grouped under it.
/// </summary>
public class SeasonResponse : BaseEntityResponse
{
    /// <summary>
    /// The display name of the season, e.g. "Temporada XXVII".
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public season links.
    /// </summary>
    public required string Slug { get; set; }

    /// <summary>
    /// The calendar year the season is played in (optional).
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// The tournaments grouped under this season.
    /// </summary>
    public IEnumerable<SeasonTournamentResponse> Tournaments { get; set; } = [];
}
