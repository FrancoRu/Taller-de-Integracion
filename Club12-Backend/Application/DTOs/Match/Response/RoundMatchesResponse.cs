using System.Collections.Generic;

namespace Application.DTOs.Match.Response;

/// <summary>
/// A single matchday and its matches, letting the frontend group the fixture by round instead of date.
/// </summary>
public class RoundMatchesResponse
{
    /// <summary>
    /// The 1-based round number; null groups matches with no round-robin matchday.
    /// </summary>
    public int? Round { get; set; }

    /// <summary>
    /// The matches played in this round, in a stable order.
    /// </summary>
    public List<DetailedMatchResponse> Matches { get; set; } = [];
}
