using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.PlayerStatistic.Request;

/// <summary>
/// Request to load a whole team's scoring sheet (planilla) for a match in one
/// coherent operation (HU-71). The sum of the entries' points must equal the
/// team's final score for that match, and every listed player must be on the
/// team's roster for that season and eligible; otherwise nothing is saved.
/// </summary>
public class LoadMatchSheetRequest
{
    /// <summary>
    /// The match whose sheet is being loaded.
    /// </summary>
    [Required]
    public required Guid MatchId { get; set; }

    /// <summary>
    /// The team (home or visitor of the match) whose players are being loaded.
    /// </summary>
    [Required]
    public required Guid TeamId { get; set; }

    /// <summary>
    /// The per-player points. A player may score 0; not every player must be
    /// listed, but the listed points must add up to the team's score.
    /// </summary>
    [Required]
    public required List<PlayerScoreEntry> Scores { get; set; } = [];
}

/// <summary>
/// A single player's points within a team's match sheet (HU-71).
/// </summary>
public class PlayerScoreEntry
{
    /// <summary>
    /// The player who scored.
    /// </summary>
    [Required]
    public required Guid PlayerId { get; set; }

    /// <summary>
    /// The points the player scored in the match (may be zero).
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Points must be a non-negative number.")]
    public required int Points { get; set; }
}
