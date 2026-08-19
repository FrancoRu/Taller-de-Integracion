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


    Task<bool> AllTeamsAssignedToStage(Guid stageId, List<Guid> TeamIds);
}
