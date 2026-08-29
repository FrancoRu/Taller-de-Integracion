using System;

namespace Application.DTOs.Champions.Response;

/// <summary>
/// The final podium of a single competition (a zone division, or the
/// cross-division cup — each counts as its own competition). When the
/// division has a playoff, the places come from the top cup's Final and
/// third-place match; otherwise they come from the top three of the
/// group-phase standings. Any place that is not yet decided is null: the
/// podium never guesses an unplayed result.
/// </summary>
public class PodiumResponse
{
    /// <summary>The id of the division this podium belongs to.</summary>
    public required Guid DivisionId { get; set; }

    /// <summary>The name of the division this podium belongs to.</summary>
    public required string DivisionName { get; set; }

    /// <summary>
    /// True when the division has at least one elimination stage, so its
    /// champion is decided by a playoff Final rather than by the group
    /// standings.
    /// </summary>
    public bool HasPlayoff { get; set; }

    /// <summary>The champion (1st place), or null when not yet decided.</summary>
    public PodiumTeamResponse? First { get; set; }

    /// <summary>The runner-up (2nd place), or null when not yet decided.</summary>
    public PodiumTeamResponse? Second { get; set; }

    /// <summary>
    /// The third place, or null when there is no third-place match / the
    /// standings do not have a third team yet.
    /// </summary>
    public PodiumTeamResponse? Third { get; set; }
}
