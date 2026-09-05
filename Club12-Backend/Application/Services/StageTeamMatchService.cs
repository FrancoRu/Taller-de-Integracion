using Application.Interfaces.Repositories;
using Application.Interfaces.Services;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Application.Services;

public class StageTeamMatchService(IStageTeamMatchRepository stageTeamMatchRepository) : IStageTeamMatchService
{
    public async Task<List<StageTeamMatch>> GetAllStageTeamMatchByStageId(Guid stageId)
    {
        return [.. await stageTeamMatchRepository.FindAsync(stm => stm.StageId == stageId)];
    }

    public async Task InsertRangeAsync(List<StageTeamMatch> stageTeamMatches)
    {
        await stageTeamMatchRepository.AddRangeAsync(stageTeamMatches);
    }

    public async Task RemoveWhereAsync(Expression<Func<StageTeamMatch, bool>> predicate)
    {
        await stageTeamMatchRepository.RemoveAsync(predicate);
    }

    /// <summary>
    /// True only when every id in <paramref name="TeamIds"/> has a matching
    /// assignment row for the stage. Relies on the caller passing distinct
    /// ids — a duplicate would inflate the expected count past the number of
    /// assignable rows and make a genuinely-incomplete assignment read as complete.
    /// </summary>
    public async Task<bool> AllTeamsAssignedToStage(Guid stageId, List<Guid> TeamIds)
    {
        return await stageTeamMatchRepository.CountAsync(stm => stm.StageId == stageId
                && TeamIds.Contains(stm.TeamId)) == TeamIds.Count;
    }
}
