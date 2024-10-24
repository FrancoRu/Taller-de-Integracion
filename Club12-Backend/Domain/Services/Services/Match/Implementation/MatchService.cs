using Entities.DTOs.Abstract;
using Entities.DTOs.Match;
using Entities.Models.MatchEntity;

using Microsoft.EntityFrameworkCore;

using Services.DataAccessLayer.GenericEntity;
using Services.Utils.OrderFiltering;

using System.Linq.Expressions;

namespace Services.Services.MatchService.Implementation;

public class MatchService(IGenericService<Match> genericMatchService) : IMatchService
{
    public Match CreateMatch(Match matchEntity)
    {
        genericMatchService.Insert(matchEntity);
        return matchEntity;
    }

    public Match? GetMatchById(Guid matchId)
    {
        return genericMatchService.FilterByExpression(match => match.Id == matchId)
                                  .Include(match => match.HomeTeam)
                                  .Include(match => match.VisitorTeam)
                                  .Include(match => match.Division)
                                  .FirstOrDefault();
    }

    public void DeleteMatch(Match matchEntity)
    {
        genericMatchService.Delete(matchEntity);
    }

    public async Task<bool> UpdateMatchAsync(Match matchEntity)
    {
        try
        {
            await genericMatchService.UpdateAsync(matchEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<PaginatedResponse<Match>> GetAllMatchesAsync(GetMatchesFilteredRequest filter)
    {
        Expression<Func<Match, bool>> expression = QueryableExtensions.ConstructFilterExpression<Match, GetMatchesFilteredRequest>(filter);
        IQueryable<Match> filteredMatches = genericMatchService.FilterByExpressionWithPagination(expression, filter, match => match.HomeTeam,
                                                                                                                     match => match.VisitorTeam,
                                                                                                                     match => match.Division)
                                                                                                .SortBy(filter);
        int totalCount = await genericMatchService.GetCountAsync(expression);

        return new PaginatedResponse<Match>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = await filteredMatches.ToListAsync()
        };
    }
}
