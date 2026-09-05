using System;

namespace Application.DTOs.Champions.Response;

/// <summary>
/// The final podium of a single competition; any place not yet decided is null rather than guessed.
/// </summary>
public class PodiumResponse
{
    /// <summary>
    /// The id of the division this podium belongs to.
    /// </summary>
    public required Guid DivisionId { get; set; }

    /// <summary>
    /// The name of the division this podium belongs to.
    /// </summary>
    public required string DivisionName { get; set; }

    /// <summary>
    /// True when the division's champion is decided by a playoff Final rather than by group standings.
    /// </summary>
    public bool HasPlayoff { get; set; }

    /// <summary>
    /// The champion in first place, or null when not yet decided.
    /// </summary>
    public PodiumTeamResponse? First { get; set; }

    /// <summary>
    /// The runner-up in second place, or null when not yet decided.
    /// </summary>
    public PodiumTeamResponse? Second { get; set; }

    /// <summary>
    /// The team in third place, or null when not yet decided.
    /// </summary>
    public PodiumTeamResponse? Third { get; set; }
}
