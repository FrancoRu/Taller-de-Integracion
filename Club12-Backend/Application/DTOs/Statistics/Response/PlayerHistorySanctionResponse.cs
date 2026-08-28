using System;

namespace Application.DTOs.Statistics.Response;

/// <summary>
/// A single sanction the player received during a given season (HU-88).
/// </summary>
public class PlayerHistorySanctionResponse
{
    public required Guid SanctionId { get; set; }

    public required string Description { get; set; }

    /// <summary>Length in fechas (matchdays), per HU-75.</summary>
    public required int Duration { get; set; }

    public required DateTime IssuedDate { get; set; }

    /// <summary>The match the sanction was issued in.</summary>
    public required Guid MatchId { get; set; }
}
