using Application.Utils.Constants.Stage;
using Domain.Enums;
using System;

namespace Application.Utils.Helper.StageHelper;

public static class StageHelper
{

    public static int GetMaxTeamsForStage(StageType stageType) => stageType switch
    {
        StageType.Group => MaxTeams.GROUP,
        StageType.QuarterFinal => MaxTeams.QUARTER_FINAL,
        StageType.SemiFinal => MaxTeams.SEMI_FINAL,
        StageType.ThirdPlace => MaxTeams.THIRD_PLACE,
        StageType.Final => MaxTeams.FINAL,

        _ => throw new ArgumentException("Invalid stage type")
    };
}
