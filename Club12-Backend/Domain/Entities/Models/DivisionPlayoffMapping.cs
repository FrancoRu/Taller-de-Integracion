using System;

namespace Domain.Entities.Models;

/// <summary>
/// Maps a contiguous range of final group-stage standings positions in a
/// division to a playoff destination (HU-45). For example, in a 10-team
/// division: positions 1-4 → "Copa Oro", 5-8 → "Copa Plata", and 9-10 left
/// unmapped (no playoff). Ranges within a division must not overlap; each
/// position goes to at most one destination. The tournament wizard sends
/// these so the system can seed multiple cups automatically (HU-81).
/// </summary>
public class DivisionPlayoffMapping : EntityBase
{
    /// <summary>
    /// The division whose standings this mapping applies to.
    /// </summary>
    public Guid DivisionId { get; set; }

    /// <summary>
    /// The division whose standings this mapping applies to.
    /// </summary>
    public Division? Division { get; set; }

    /// <summary>
    /// First standings position in the range (1-based, inclusive).
    /// </summary>
    public required int FromPosition { get; set; }

    /// <summary>
    /// Last standings position in the range (1-based, inclusive). Must be
    /// greater than or equal to <see cref="FromPosition"/>.
    /// </summary>
    public required int ToPosition { get; set; }

    /// <summary>
    /// The playoff destination the teams in this range qualify for, matching
    /// the <see cref="Stage.BracketName"/> of that cup's elimination stages
    /// (e.g. "Copa Oro", "Copa Plata").
    /// </summary>
    public required string Destination { get; set; }
}
