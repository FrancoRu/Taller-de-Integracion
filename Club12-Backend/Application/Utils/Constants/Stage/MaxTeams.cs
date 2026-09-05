namespace Application.Utils.Constants.Stage;

/// <summary>
/// Provides constant values representing the maximum number of teams allowed in each stage of a tournament.
/// </summary>
public static class MaxTeams
{
    /// <summary>
    /// Standard number of teams per group used to partition a division's registered teams into same-sized Group stages.
    /// </summary>
    public const int GROUP = 4;

    /// <summary>
    /// Upper bound on how many teams a single manually-built Group stage, one zone's whole round-robin phase, may hold.
    /// </summary>
    public const int GROUP_STAGE_CAP = 32;

    public const int QUARTER_FINAL = 8;

    public const int SEMI_FINAL = 4;

    public const int THIRD_PLACE = 2;

    public const int FINAL = 2;
}
