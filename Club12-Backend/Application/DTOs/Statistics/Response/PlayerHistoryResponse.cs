using System;
using System.Collections.Generic;

namespace Application.DTOs.Statistics.Response;

/// <summary>
/// A player's full cross-season trajectory (HU-88): one entry per season the
/// player was registered, most recent first. Links the same person's
/// registrations across seasons (D2) via the stable PlayerId.
/// </summary>
public class PlayerHistoryResponse
{
    public required Guid PlayerId { get; set; }

    public required string FullName { get; set; }

    /// <summary>Per-season trajectory rows, most recent season first.</summary>
    public IEnumerable<PlayerHistorySeasonResponse> Seasons { get; set; } = [];
}
