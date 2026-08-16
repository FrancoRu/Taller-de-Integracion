using Application.DTOs.Abstract.Response;
using Application.DTOs.BlogPosts.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Extensions;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Application.Services;

public class BlogPostService(IBlogPostRepository blogpostRepository) : IBlogPostService
{
    public async Task<BlogPost> CreateBlogPostAsync(BlogPost blogPostEntity)
    {
        return await blogpostRepository.AddAsync(blogPostEntity);
    }

    public async Task DeleteBlogPostAsync(Guid id)
    {
        await blogpostRepository.RemoveAsync(bp => bp.Id == id);
    }

    public async Task UpdateBlogPostAsync(BlogPost blogPostEntity)
    {
        await blogpostRepository.UpdateAsync(blogPostEntity);
    }

    public async Task<BlogPost?> GetBlogPostByIdAsync(Guid blogPostId)
    {
        return await blogpostRepository.GetByIdAsync(blogPostId);
    }

    public async Task<PaginatedResponse<BlogPost>> GetAllBlogPostsAsync(GetBlogPostsFilteredRequest filter)
    {
        Expression<Func<BlogPost, bool>> expression = QueryableExtensions.ConstructFilterExpression<BlogPost, GetBlogPostsFilteredRequest>(filter);
        IEnumerable<BlogPost> filteredBlogPosts = await blogpostRepository.FindAsync(expression, filter: filter);

        int totalCount = await blogpostRepository.CountAsync(expression);

        return new PaginatedResponse<BlogPost>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = filteredBlogPosts
        };
    }
}
