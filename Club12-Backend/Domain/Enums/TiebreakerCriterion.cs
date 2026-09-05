namespace Domain.Enums;

/// <summary>
/// The group-stage tiebreaker criteria, in the exact priority order the club's rulebook applies them.
/// </summary>
public enum TiebreakerCriterion
{
    /// <summary>
    /// Table points awarded for wins and losses.
    /// </summary>
    Points,

    /// <summary>
    /// Games won across the whole zone.
    /// </summary>
    GamesWon,

    /// <summary>
    /// Points for minus points against, across the whole zone.
    /// </summary>
    PointsDifference,

    /// <summary>
    /// Head-to-head result among the tied teams.
    /// </summary>
    HeadToHead,

    /// <summary>
    /// Points difference considering only the games played among the tied teams.
    /// </summary>
    HeadToHeadPointsDifference,
}
