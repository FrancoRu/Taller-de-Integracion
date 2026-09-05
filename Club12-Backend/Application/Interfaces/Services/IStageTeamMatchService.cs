using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

public interface IStageTeamMatchService
{
    Task<List<StageTeamMatch>> GetAllStageTeamMatchByStageId(Guid stageId);
    Task InsertRangeAsync(List<StageTeamMatch> stageTeamMatches);
    Task RemoveWhereAsync(Expression<Func<StageTeamMatch, bool>> predicate);

    /// <summary>
    /// True only when every id in TeamIds has a matching assignment row for the stage.
    /// </summary>
    Task<bool> AllTeamsAssignedToStage(Guid stageId, List<Guid> TeamIds);
}
