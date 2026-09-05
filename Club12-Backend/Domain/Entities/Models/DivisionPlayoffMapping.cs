using System;

namespace Domain.Entities.Models;

/// <summary>
/// Maps a contiguous range of final group-stage standings positions in a division to a playoff destination.
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
    /// First, 1-based inclusive standings position in the range.
    /// </summary>
    public required int FromPosition { get; set; }

    /// <summary>
    /// Last, 1-based inclusive standings position in the range, greater than or equal to FromPosition.
    /// </summary>
    public required int ToPosition { get; set; }

    /// <summary>
    /// The playoff destination the teams in this range qualify for, matching the BracketName of that cup's elimination stages.
    /// </summary>
    public required string Destination { get; set; }
}
