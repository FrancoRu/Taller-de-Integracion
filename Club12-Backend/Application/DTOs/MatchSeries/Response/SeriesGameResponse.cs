using Application.DTOs.Match.Response;

namespace Application.DTOs.MatchSeries.Response;

/// <summary>
/// A single game within a best-of-N series, with its position in the series alongside the match details.
/// </summary>
public class SeriesGameResponse : MinimalMatchResponse
{
    /// <summary>
    /// The game's 1-based position within the series.
    /// </summary>
    public required int GameNumber { get; set; }
}
