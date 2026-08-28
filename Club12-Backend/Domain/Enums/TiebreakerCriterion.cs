namespace Domain.Enums;

/// <summary>
/// The group-stage tiebreaker criteria (HU-80), in the exact priority order
/// the club's rulebook applies them. A team's placement is resolved by the
/// first criterion at which it separates from the team ranked immediately
/// above it, and that criterion can be surfaced in the standings UI.
/// </summary>
public enum TiebreakerCriterion
{
    /// <summary>Table points (PTS): points awarded for wins/losses.</summary>
    Points,

    /// <summary>Games won (PG) across the whole zone.</summary>
    GamesWon,

    /// <summary>Points difference (DG): points for minus against, whole zone.</summary>
    PointsDifference,

    /// <summary>Head-to-head result among the tied teams (mini-table).</summary>
    HeadToHead,

    /// <summary>
    /// Points difference considering only the games played among the tied
    /// teams. Only meaningful when the tied teams played each other more
    /// than once.
    /// </summary>
    HeadToHeadPointsDifference,
}
