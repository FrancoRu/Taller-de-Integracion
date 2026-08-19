using Application.DTOs.Abstract.Response;
using Application.DTOs.BlogPosts.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Extensions;
using Application.Utils.Helper.Slug;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Application.Services;

public class BlogPostService(IBlogPostRepository blogpostRepository) : IBlogPostService
{
    public async Task<BlogPost> CreateBlogPostAsync(BlogPost blogPostEntity)
    {
        blogPostEntity.Slug = await SlugGenerator.GenerateUniqueSlugAsync(
            blogPostEntity.Title,
            candidate => blogpostRepository.ExistsAsync(post => post.Slug == candidate));

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

    /// <summary>
    /// Retrieves a blog post by its id or its slug. The value is treated as
    /// an id when it parses as a GUID, otherwise it is looked up as a slug.
    /// </summary>
    /// <param name="idOrSlug">The blog post's GUID id or its slug.</param>
    /// <returns>The blog post entity if found; otherwise, null.</returns>
    public async Task<BlogPost?> GetBlogPostByIdOrSlugAsync(string idOrSlug)
    {
        if (Guid.TryParse(idOrSlug, out Guid blogPostId))
        {
            return await GetBlogPostByIdAsync(blogPostId);
        }

        IEnumerable<BlogPost> posts = await blogpostRepository.FindAsync(post => post.Slug == idOrSlug);
        return posts.FirstOrDefault();
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
