using Entities.DTOs.Abstract;
using Entities.DTOs.BlogPost;
using Entities.Models.BlogPostEntity;

using Microsoft.EntityFrameworkCore;

using Services.DataAccessLayer.GenericEntity;
using Services.Utils.OrderFiltering;

using System.Linq.Expressions;

namespace Services.Services.BlogPostService.Implementation;

public class BlogPostService(IGenericService<BlogPost> genericBlogPostService) : IBlogPostService
{
    public BlogPost CreateBlogPost(BlogPost blogPostEntity)
    {
        genericBlogPostService.Insert(blogPostEntity);
        return blogPostEntity;
    }

    public void DeleteBlogPost(BlogPost blogPostEntity)
    {
        genericBlogPostService.Delete(blogPostEntity);
    }

    public async Task<bool> UpdateBlogPostAsync(BlogPost blogPostEntity)
    {
        try
        {
            await genericBlogPostService.UpdateAsync(blogPostEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public BlogPost? GetBlogPostById(Guid blogPostId)
    {
        return genericBlogPostService.FilterByExpression(blogPost => blogPost.Id == blogPostId)
                                     .FirstOrDefault();
    }

    public async Task<PaginatedResponse<BlogPost>> GetAllBlogPostsAsync(GetBlogPostsFilteredRequest filter)
    {
        Expression<Func<BlogPost, bool>> expression = QueryableExtensions.ConstructFilterExpression<BlogPost, GetBlogPostsFilteredRequest>(filter);
        IQueryable<BlogPost> filteredBlogPosts = genericBlogPostService.FilterByExpressionWithPagination(expression, filter)
                                                                                     .SortBy(filter);

        int totalCount = await genericBlogPostService.GetCountAsync(expression);

        return new PaginatedResponse<BlogPost>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = await filteredBlogPosts.ToListAsync()
        };
    }
}
