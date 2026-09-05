using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.PlayerStatistic.Request;

/// <summary>
/// Request to load a whole team's scoring sheet for a match in one coherent operation.
/// </summary>
public class LoadMatchSheetRequest
{
    /// <summary>
    /// The match whose sheet is being loaded.
    /// </summary>
    [Required]
    public required Guid MatchId { get; set; }

    /// <summary>
    /// The home or visitor team whose players are being loaded.
    /// </summary>
    [Required]
    public required Guid TeamId { get; set; }

    /// <summary>
    /// The per-player points, which must add up to the team's score; a player may score 0 and need not be listed.
    /// </summary>
    [Required]
    public required List<PlayerScoreEntry> Scores { get; set; } = [];
}
