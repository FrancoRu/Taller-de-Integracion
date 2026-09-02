using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.PlayerStatistic.Request;

/// <summary>
/// Request to finish a match by loading BOTH teams' scoring sheets in one
/// coherent operation (HU-72): the match's final score is DERIVED as the sum
/// of each team's listed player points, rather than typed in separately and
/// then checked against a sheet — a game's score is, definitionally, the
/// total of what its players scored. Every listed player must be on their
/// team's roster for that season and eligible, and the two sums must not tie
/// (HU-70); otherwise nothing is saved.
/// </summary>
public class LoadMatchResultFromSheetsRequest
{
    /// <summary>
    /// The match being finished.
    /// </summary>
    [Required]
    public required Guid MatchId { get; set; }

    /// <summary>
    /// The home team's per-player points. A player may score 0; not every
    /// roster player must be listed.
    /// </summary>
    [Required]
    public required List<PlayerScoreEntry> HomeScores { get; set; } = [];

    /// <summary>
    /// The visitor team's per-player points.
    /// </summary>
    [Required]
    public required List<PlayerScoreEntry> VisitorScores { get; set; } = [];

    /// <summary>
    /// Whether the match was decided in overtime (basketball rule). Purely
    /// informational; defaults to false.
    /// </summary>
    public bool WentToOvertime { get; set; }
}
