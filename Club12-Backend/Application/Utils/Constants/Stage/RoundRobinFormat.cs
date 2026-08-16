namespace Application.Utils.Constants.Stage;

/// <summary>
/// Bounds for how many times each pair of teams plays within a single
/// group stage (RoundRobinLegs).
/// </summary>
public static class RoundRobinFormat
{
    /// <summary>
    /// Single round-robin: every pair plays once.
    /// </summary>
    public const int MIN_LEGS = 1;

    /// <summary>
    /// Upper bound to keep fixture size sane (triple round-robin).
    /// </summary>
    public const int MAX_LEGS = 3;
}
