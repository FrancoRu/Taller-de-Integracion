using Entities.DTOs.Abstract;
using Entities.DTOs.BlogPosts;
using Entities.Models.BlogPosts;

using Microsoft.EntityFrameworkCore;

using Services.DataAccessLayer.GenericEntity;
using Services.Utils.OrderFiltering;

using System.Linq.Expressions;

namespace Services.Services.BlogPosts.Implementation;

public class BlogPostService(IGenericService<BlogPost> _genericBlogPostService) : IBlogPostService
{
    public async Task<BlogPost> CreateBlogPostAsync(BlogPost blogPostEntity)
    {
        await _genericBlogPostService.InsertAsync(blogPostEntity);
        return blogPostEntity;
    }

    public async Task<bool> DeleteBlogPostAsync(BlogPost blogPostEntity)
    {
        try
        {
            await _genericBlogPostService.DeleteAsync(blogPostEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateBlogPostAsync(BlogPost blogPostEntity)
    {
        try
        {
            await _genericBlogPostService.UpdateAsync(blogPostEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<BlogPost?> GetBlogPostByIdAsync(Guid blogPostId) => await _genericBlogPostService.FilterByExpression(blogPost => blogPost.Id == blogPostId)
                                            .FirstOrDefaultAsync();

    public async Task<PaginatedResponse<BlogPost>> GetAllBlogPostsAsync(GetBlogPostsFilteredRequest filter)
    {
        Expression<Func<BlogPost, bool>> expression = QueryableExtensions.ConstructFilterExpression<BlogPost, GetBlogPostsFilteredRequest>(filter);
        IQueryable<BlogPost> filteredBlogPosts = _genericBlogPostService.FilterByExpressionWithPagination(expression, filter)
                                                                                                         .SortBy(filter);

        int totalCount = await _genericBlogPostService.GetCountAsync(expression);

        return new PaginatedResponse<BlogPost>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = await filteredBlogPosts.ToListAsync()
        };
    }
}
