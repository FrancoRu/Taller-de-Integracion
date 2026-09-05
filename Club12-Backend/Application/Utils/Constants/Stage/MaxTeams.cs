namespace Application.Utils.Constants.Stage;

public static class MaxTeams
{
    /// <summary>
    /// Standard number of teams per group used to partition a division's registered teams into same-sized Group stages.
    /// </summary>
    public const int Group = 4;

    /// <summary>
    /// Upper bound on how many teams a single manually-built Group stage, one zone's whole round-robin phase, may hold.
    /// </summary>
    public const int GroupStageCap = 32;

    public const int QuarterFinal = 8;

    public const int SemiFinal = 4;

    public const int ThirdPlace = 2;

    public const int Final = 2;
}
