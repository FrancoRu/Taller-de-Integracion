using System;

namespace Application.DTOs.Match.Request;

/// <summary>
/// Request to reschedule a match; edits only the calendar date and venue.
/// </summary>
public class UpdateMatchRequest
{
    public DateTime? MatchDate { get; set; }

    public Guid? VenueId { get; set; }
}
