using System;

namespace Application.DTOs.Match.Request;

/// <summary>
/// Request to suspend a match and optionally reschedule it, never changing its round.
/// </summary>
public class SuspendMatchRequest
{
    /// <summary>
    /// Optional new date and time for the match; null suspends it in place without rescheduling.
    /// </summary>
    public DateTime? MatchDate { get; set; }
}
