namespace Application.Utils.Constants.Stage;

/// <summary>
/// Provides constant values representing the maximum number of teams allowed in each stage of a tournament.
/// </summary>
public static class MaxTeams
{
    /// <summary>
    /// Standard number of teams per group used by
    /// <see cref="Application.Services.StageService.CreateAutomatedStagesAsync"/>
    /// to partition a division's registered teams into same-sized Group
    /// stages (registeredTeams / GROUP groups). NOT a general "how many
    /// teams can a Group-type stage ever hold" cap — a single Group stage
    /// manually built by the tournament wizard (one per zone, arbitrary
    /// team count) is a different shape entirely and is intentionally NOT
    /// bounded by this constant; see the Group case carved out in
    /// StageService.AssignTeamsToStageAsync.
    /// </summary>
    public const int GROUP = 4;

    /// <summary>
    /// Upper bound on how many teams a single manually-built Group stage (one
    /// zone's whole round-robin phase) may hold. Used only as a sanity ceiling
    /// in <see cref="Application.Services.StageService.AssignTeamsToStageAsync"/>;
    /// unrelated to the auto-generator's fixed per-group <see cref="GROUP"/> size.
    /// </summary>
    public const int GROUP_STAGE_CAP = 32;

    public const int QUARTER_FINAL = 8;

    public const int SEMI_FINAL = 4;

    public const int THIRD_PLACE = 2;

    public const int FINAL = 2;
}
