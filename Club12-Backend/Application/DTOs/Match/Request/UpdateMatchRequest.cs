using System;

namespace Application.DTOs.Match.Request;

/// <summary>
/// Represents a request to reschedule a match: it edits ONLY the calendar date
/// and the venue. The teams (HomeTeamId/VisitorTeamId) are intentionally NOT
/// part of this contract — they are set when the fixture is built and must never
/// be touched here. Including them once caused a data-loss bug: an "edit
/// date/venue" call (which never sends team ids) mapped their null values over
/// the entity and wiped both teams off the match.
/// </summary>
public class UpdateMatchRequest
{
    public DateTime? MatchDate { get; set; }

    public Guid? VenueId { get; set; }
}
