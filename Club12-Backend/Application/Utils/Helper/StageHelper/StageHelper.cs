using Application.Utils.Constants;
using Application.Utils.Constants.Stage;

using Domain.Enums;

using System;

namespace Application.Utils.Helper.StageHelper;
public static class StageHelper
{
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
            _ => throw new ArgumentException(ErrorMessages.Stage.InvalidStageType)
        };
    }
}