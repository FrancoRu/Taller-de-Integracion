using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.PlayerStatistic.Request;

/// <summary>
/// Request to finish a match by loading both teams' scoring sheets in one coherent operation.
/// </summary>
public class LoadMatchResultFromSheetsRequest
{
    /// <summary>
    /// The match being finished, assigned server-side from the route rather than sent by the client.
    /// </summary>
    public Guid MatchId { get; set; }

    /// <summary>
    /// The home team's per-player points; a player may score 0 and not every roster player must be listed.
    /// </summary>
    [Required]
    public required List<PlayerScoreEntry> HomeScores { get; set; } = [];

    /// <summary>
    /// The visitor team's per-player points.
    /// </summary>
    [Required]
    public required List<PlayerScoreEntry> VisitorScores { get; set; } = [];

    /// <summary>
    /// Whether the match was decided in overtime; purely informational and defaults to false.
    /// </summary>
    public bool WentToOvertime { get; set; }
}
