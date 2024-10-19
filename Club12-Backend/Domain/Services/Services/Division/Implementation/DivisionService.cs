using Entities.DTOs.Abstract;
using Entities.DTOs.Division;
using Entities.Models.DivisionEntity;
using Microsoft.EntityFrameworkCore;
using Services.DataAccessLayer.GenericEntity;
using Services.Utils.OrderFiltering;
using System.Linq.Expressions;

namespace Services.Services.DivisionService.Implementation;

public class DivisionService(IGenericService<Division> genericDivisionService) : IDivisionService
{
    public Division CreateDivision(Division divisionEntity)
    {
        genericDivisionService.Insert(divisionEntity);
        return divisionEntity;
    }

    public void DeleteDivision(Division divisionEntity)
    {
        genericDivisionService.Delete(divisionEntity);
    }

    public async Task<bool> UpdateDivision(Division divisionEntity)
    {
        try
        {
            await genericDivisionService.UpdateAsync(divisionEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public Division? GetDivisionById(Guid divisionId)
    {
        return genericDivisionService.FilterByExpression(division => division.Id == divisionId)
                                     .Include(division => division.Teams)
                                     .FirstOrDefault();
    }

    public async Task<PaginatedResponse<Division>> GetAllDivisionsAsync(GetDivisionsFilteredRequest filter)
    {
        Expression<Func<Division, bool>> expression = QueryableExtensions.ConstructFilterExpression<Division, GetDivisionsFilteredRequest>(filter);
        IQueryable<Division> filteredDivisions = genericDivisionService.FilterByExpressionWithPagination(expression, filter, division => division.Teams).SortBy(filter);
        int totalCount = await genericDivisionService.GetCountAsync(expression);

        return new PaginatedResponse<Division>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = await filteredDivisions.ToListAsync()
        };
    }
}
