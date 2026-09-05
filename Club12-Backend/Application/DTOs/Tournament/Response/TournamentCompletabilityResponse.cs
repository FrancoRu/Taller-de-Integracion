using System.Collections.Generic;

namespace Application.DTOs.Tournament.Response;

/// <summary>
/// The completability report for a tournament: whether it can be started, and the blocking issues if not.
/// </summary>
public class TournamentCompletabilityResponse
{
    /// <summary>
    /// True when the tournament has no completability issues and may be started.
    /// </summary>
    public bool CanStart { get; set; }

    /// <summary>
    /// Every completability violation found; empty when CanStart is true.
    /// </summary>
    public List<CompletabilityIssue> Issues { get; set; } = [];
}
