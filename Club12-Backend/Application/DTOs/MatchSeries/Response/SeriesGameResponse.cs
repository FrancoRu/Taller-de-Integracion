using Application.DTOs.Match.Response;

namespace Application.DTOs.MatchSeries.Response;

/// <summary>
/// A single game within a best-of-N series, including its position in
/// the series alongside the usual match details.
/// </summary>
public class SeriesGameResponse : MinimalMatchResponse
{
    /// <summary>
    /// The game's position within the series (1-based).
    /// </summary>
    public required int GameNumber { get; set; }
}
