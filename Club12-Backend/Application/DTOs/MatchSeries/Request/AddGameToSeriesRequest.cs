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
    [Required(ErrorMessage = "Match date field is required.")]
    public required DateTime MatchDate { get; set; }

    public Guid? VenueId { get; set; }
}
