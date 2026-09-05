namespace Application.Utils.Constants.Stage;

/// <summary>
/// Bounds for how many times each pair of teams plays within a single group stage, RoundRobinLegs.
/// </summary>
public static class RoundRobinFormat
{
    /// <summary>
    /// Single round-robin: every pair plays once.
    /// </summary>
    public const int MinLegs = 1;

    /// <summary>
    /// Upper bound to keep fixture size sane, a triple round-robin.
    /// </summary>
    public const int MaxLegs = 3;
}
