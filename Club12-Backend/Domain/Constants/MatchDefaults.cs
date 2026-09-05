namespace Domain.Constants;

/// <summary>
/// Regulation defaults applied to matches when no explicit score is entered.
/// </summary>
public static class MatchDefaults
{
    /// <summary>
    /// Default score awarded to the present team on a walkover.
    /// </summary>
    public const int WalkOverWinnerScore = 20;

    /// <summary>
    /// Score assigned to the absent team on a walkover.
    /// </summary>
    public const int WalkOverLoserScore = 0;
}
