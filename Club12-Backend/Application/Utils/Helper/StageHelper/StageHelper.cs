using Application.Utils.Constants.Stage;

using Domain.Enums;

using System;

namespace Application.Utils.Helper.StageHelper;
/// <summary>
/// Provides helper methods for stage-related operations.
/// </summary>
public static class StageHelper
{
    /// <summary>
    /// Returns the maximum number of teams allowed for a given stage type.
    /// </summary>
    /// <param name="stageType">The type of stage for which to retrieve the maximum number of teams.</param>
    /// <returns>The maximum number of teams allowed for the specified stage type.</returns>
    /// <exception cref="ArgumentException">Thrown when an invalid stage type is provided.</exception>
    public static int GetMaxTeamsForStage(StageType stageType)
    {
        return stageType switch
        {
            StageType.Group => MaxTeams.GROUP,
            StageType.QuarterFinal => MaxTeams.QUARTER_FINAL,
            StageType.SemiFinal => MaxTeams.SEMI_FINAL,
            StageType.ThirdPlace => MaxTeams.THIRD_PLACE,
            StageType.Final => MaxTeams.FINAL,
            _ => throw new ArgumentException("Invalid stage type")
        };
    }
}