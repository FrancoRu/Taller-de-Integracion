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
    /// Maximum number of teams in the quarter-final stage.
    /// </summary>
    public const int QUARTER_FINAL = 8;

    /// <summary>
    /// Maximum number of teams in the semi-final stage.
    /// </summary>
    public const int SEMI_FINAL = 4;

    /// <summary>
    /// Maximum number of teams in the third-place match.
    /// </summary>
    public const int THIRD_PLACE = 2;

    /// <summary>
    /// Maximum number of teams in the final match.
    /// </summary>
    public const int FINAL = 2;
}
