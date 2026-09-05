using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.MatchSeries.Request;

/// <summary>
/// Payload for scheduling the next game of an existing series; GameNumber and teams are derived server-side.
/// </summary>
public class AddGameToSeriesRequest
{
    [Required(ErrorMessage = "Match date field is required.")]
    public required DateTime MatchDate { get; set; }

    public Guid? VenueId { get; set; }
}
