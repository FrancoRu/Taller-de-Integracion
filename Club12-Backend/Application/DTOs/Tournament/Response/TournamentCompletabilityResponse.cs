using System.Collections.Generic;

namespace Application.DTOs.Tournament.Response;

/// <summary>
/// The completability report for a tournament (HU-109): whether it can be
/// started, and the list of blocking issues when it cannot. An empty
/// <see cref="Issues"/> list means <see cref="CanStart"/> is true and starting
/// the tournament (transition to Ongoing) will not be blocked by the guard.
/// </summary>
public class TournamentCompletabilityResponse
{
    /// <summary>
    /// True when the tournament has no completability issues and may be started.
    /// </summary>
    public bool CanStart { get; set; }

    /// <summary>
    /// Every completability violation found; empty when
    /// <see cref="CanStart"/> is true.
    /// </summary>
    public List<CompletabilityIssue> Issues { get; set; } = [];
}
