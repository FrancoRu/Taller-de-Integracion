namespace Domain.Constants;

/// <summary>
/// Regulation defaults applied to matches when no explicit score is entered.
/// </summary>
public static class MatchDefaults
{
    /// <summary>
    /// Default score awarded to the present team on a walkover (HU-73). FIBA
    /// scores a walkover 20-0; the loading endpoint may override the winner's
    /// score, but the absent team always gets zero.
    /// </summary>
    public const int WalkOverWinnerScore = 20;

    /// <summary>
    /// Score assigned to the absent team on a walkover (HU-73).
    /// </summary>
    public const int WalkOverLoserScore = 0;
}
