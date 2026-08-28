using System;

namespace Application.DTOs.Match.Request;

/// <summary>
/// Represents a request to reprogram/suspend a match (HU-68). The match is
/// marked suspended and, when a new date is provided, moved to it — never
/// changing its round (HU-67).
/// </summary>
public class SuspendMatchRequest
{
    /// <summary>
    /// Optional new calendar date/time for the match. When null, the match is
    /// suspended in place without rescheduling.
    /// </summary>
    public DateTime? MatchDate { get; set; }
}
