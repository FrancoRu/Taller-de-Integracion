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

/// <summary>
/// Manages blog posts and keeps draft visibility opt-in so a public endpoint can never leak a draft.
/// </summary>
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
    /// Retrieves a blog post by id or slug, auto-detecting which one was passed via GUID parsing.
    /// </summary>
    /// <param name="idOrSlug">The blog post's GUID id or its slug.</param>
    /// <param name="includeUnpublished">
    /// Defaults to false, which treats a draft as not found so it never leaks through public
    /// endpoints; true also resolves drafts.
    /// </param>
    /// <returns>The blog post entity if found; otherwise, null.</returns>
    public async Task<BlogPost?> GetBlogPostByIdOrSlugAsync(string idOrSlug, bool includeUnpublished = false)
    {
        BlogPost? post = Guid.TryParse(idOrSlug, out Guid blogPostId)
            ? await GetBlogPostByIdAsync(blogPostId)
            : (await blogpostRepository.FindAsync(candidate => candidate.Slug == idOrSlug)).FirstOrDefault();

        // Public callers must not resolve drafts: a draft is treated as not found so it never leaks through the public detail endpoint.
        if (post is not null && !includeUnpublished && !post.IsPublished)
        {
            return null;
        }

        return post;
    }

    public async Task<PaginatedResponse<BlogPost>> GetAllBlogPostsAsync(GetBlogPostsFilteredRequest filter, bool includeUnpublished = false)
    {
        Expression<Func<BlogPost, bool>> expression = QueryableExtensions.ConstructFilterExpression<BlogPost, GetBlogPostsFilteredRequest>(filter);

        // Public listing only ever exposes published posts; Admin/Owner callers pass includeUnpublished: true to also see drafts.
        if (!includeUnpublished)
        {
            Expression<Func<BlogPost, bool>> publishedOnly = post => post.IsPublished;
            expression = expression.AndAlso(publishedOnly);
        }

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
