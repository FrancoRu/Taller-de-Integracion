using Entities.DTOs.Abstract;
using Entities.DTOs.Stage;
using Entities.Models.Stages;
using Microsoft.EntityFrameworkCore;
using Services.DataAccessLayer.GenericEntity;
using Services.Utils.OrderFiltering;
using System.Linq.Expressions;

namespace Services.Services.Stages.Implementation;

public class StageService(IGenericService<Stage> _genericStageService) : IStageService
{
    public async Task<Stage?> GetStageByIdAsync(Guid stageId) 
        => await _genericStageService.FilterByExpression(stage => stage.Id == stageId).FirstOrDefaultAsync();

    public async Task<PaginatedResponse<Stage>> GetAllStagesAsync(GetStagesFilteredRequest filter)
    {
        Expression<Func<Stage, bool>> expression = QueryableExtensions.ConstructFilterExpression<Stage, GetStagesFilteredRequest>(filter);
        IQueryable<Stage> filteredPlayers = _genericStageService.FilterByExpressionWithPagination(expression, filter).SortBy(filter);
        int totalCount = await _genericStageService.GetCountAsync(expression);

        return new PaginatedResponse<Stage>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = await filteredPlayers.ToListAsync()
        };
    }
    public async Task<bool> DeleteStageAsync(Stage stageEntity)
    {
        try
        {
            await _genericStageService.DeleteAsync(stageEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<Stage> UpdateStageAsync(Stage stageEntity)
    {
        try
        {
            await _genericStageService.UpdateAsync(stageEntity);
            return stageEntity;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Stage> CreateStageAsync(Stage stageEntity)
    {
        await _genericStageService.InsertAsync(stageEntity);
        return stageEntity;
    }

}
