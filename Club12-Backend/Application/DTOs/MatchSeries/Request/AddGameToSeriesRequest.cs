using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.MatchSeries.Request;

/// <summary>
/// Represents the payload for scheduling the next game of an existing
/// series. GameNumber and team assignment are derived server-side from
/// the series; only game-specific details are provided here.
/// </summary>
public class AddGameToSeriesRequest
{
    /// <summary>
    /// The date of this game.
    /// </summary>
    [Required(ErrorMessage = "Match date field is required.")]
    public required DateTime MatchDate { get; set; }

    /// <summary>
    /// Optional venue for this game.
    /// </summary>
    public Guid? VenueId { get; set; }
}
