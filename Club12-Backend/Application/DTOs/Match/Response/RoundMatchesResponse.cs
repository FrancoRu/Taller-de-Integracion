using System.Collections.Generic;

namespace Application.DTOs.Match.Response;

/// <summary>
/// A single matchday (jornada) and the matches played in it (HU-63). Returned
/// as an ordered list so the frontend can render the fixture grouped by round
/// ("Fecha 1 / Partido 1..2, Fecha 2 / …") instead of by calendar date.
/// </summary>
public class RoundMatchesResponse
{
    /// <summary>
    /// The 1-based round number. Null groups matches that have no round-robin
    /// matchday (e.g. knockout stages).
    /// </summary>
    public int? Round { get; set; }

    /// <summary>
    /// The matches played in this round, in a stable order.
    /// </summary>
    public List<DetailedMatchResponse> Matches { get; set; } = [];
}
