using Application.DTOs.Abstract.Response;
using Application.DTOs.Divisions.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Extensions;
using Domain.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Application.Services;

public class DivisionService(IDivisionRepository divisionRepository) : IDivisionService
{
    public async Task<Division> CreateDivisionAsync(Division divisionEntity)
    {
        await divisionRepository.AddAsync(divisionEntity);
        return divisionEntity;
    }

    public async Task DeleteDivisionAsync(Guid id)
        => await divisionRepository.RemoveAsync(division => division.Id == id);
    

    public async Task UpdateDivisionAsync(Division divisionEntity)
        => await divisionRepository.UpdateAsync(divisionEntity);

    public async Task<Division?> GetSimpleDivisionByIdAsync(Guid divisionId)
        => await divisionRepository.GetByIdAsync(divisionId);

    public async Task<Division?> GetFullDivisionByIdAsync(Guid divisionId)
        => await divisionRepository.GetByIdAsync(divisionId, includes: [division => division.Tournament, division => division.Stages]);

    public async Task<PaginatedResponse<Division>> GetAllDivisionsAsync(GetDivisionsFilteredRequest filter)
    {
        Expression<Func<Division, bool>> expression = QueryableExtensions.ConstructFilterExpression<Division, GetDivisionsFilteredRequest>(filter);
        IEnumerable<Division> filteredDivisions = await divisionRepository.FindAsync(expression, filter: filter);

        int totalCount = await divisionRepository.CountAsync(expression);

        return new PaginatedResponse<Division>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = filteredDivisions
        };
    }
}
