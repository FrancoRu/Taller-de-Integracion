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
            StageType.Group => MaxTeams.Group,
            StageType.QuarterFinal => MaxTeams.QuarterFinal,
            StageType.SemiFinal => MaxTeams.SemiFinal,
            StageType.ThirdPlace => MaxTeams.ThirdPlace,
            StageType.Final => MaxTeams.Final,
            _ => throw new ArgumentException(ErrorMessages.Stage.InvalidStageType)
        };
    }
}