using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Application.Services;

public class StageTeamMatchService(IStageTeamMatchRepository stageTeamMatchRepository): IStageTeamMatchService
{
    public async Task<List<StageTeamMatch>> GetAllStageTeamMatchByStageId(Guid stageId)
        => [.. (await stageTeamMatchRepository.FindAsync(stm => stm.StageId == stageId))];

    public async Task InsertRangeAsync(List<StageTeamMatch> stageTeamMatches) 
        => await stageTeamMatchRepository.AddRangeAsync(stageTeamMatches);

    public async Task RemoveWhereAsync(Expression<Func<StageTeamMatch, bool>> predicate) 
        => await stageTeamMatchRepository.RemoveAsync(predicate);

    public async Task<bool> AllTeamsAssignedToStage(Guid stageId, List<Guid> teamsId)
        => await stageTeamMatchRepository.CountAsync(stm => stm.StageId == stageId 
            && teamsId.Contains(stm.TeamId)) == teamsId.Count;
}
