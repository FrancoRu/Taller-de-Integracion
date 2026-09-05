namespace Application.Utils.Constants.Scorer;

/// <summary>
/// Points awarded per match outcome when computing the team scoreboard
/// (<see cref="Application.Utils.Mappers.ScorerMapper"/>). Not the usual
/// 3-1-0 scheme: a loss still awards 1 point, the same as a draw, so only a
/// win is worth more than just having played.
/// </summary>
public static class ScoreConstants
{
    public const int POINTS_FOR_WIN = 2;
    public const int POINTS_FOR_LOSS = 1;
    public const int POINTS_FOR_DRAW = 1;
}
