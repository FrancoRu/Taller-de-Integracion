using System;
using System.Collections.Generic;

namespace Application.DTOs.Statistics.Response;

/// <summary>
/// A player's full cross-season trajectory, one entry per registered season, most recent first.
/// </summary>
public class PlayerHistoryResponse
{
    public required Guid PlayerId { get; set; }

    public required string FullName { get; set; }

    /// <summary>
    /// Per-season trajectory rows, most recent season first.
    /// </summary>
    public IEnumerable<PlayerHistorySeasonResponse> Seasons { get; set; } = [];
}
