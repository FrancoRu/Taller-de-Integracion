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
    /// True only when every id in <paramref name="TeamIds"/> has a matching
    /// assignment row for the stage. Assumes the caller passes distinct ids —
    /// a duplicate inflates the expected count past the number of assignable
    /// rows and can make a genuinely-incomplete assignment read as complete.
    /// </summary>
    Task<bool> AllTeamsAssignedToStage(Guid stageId, List<Guid> TeamIds);
}
