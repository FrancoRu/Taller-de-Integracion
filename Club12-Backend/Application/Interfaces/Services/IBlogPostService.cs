using Application.DTOs.Abstract.Response;
using Application.DTOs.BlogPosts.Request;

using Domain.Entities.Models;

using System;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

public interface IBlogPostService
{
    /// <summary>
    /// Creates a blog post, deriving a unique slug from its title.
    /// </summary>
    /// <param name="blogPostEntity">The blog post entity to create.</param>
    /// <returns>The created blog post.</returns>
    Task<BlogPost> CreateBlogPostAsync(BlogPost blogPostEntity);

    Task<BlogPost?> GetBlogPostByIdAsync(Guid blogPostId);

    /// <summary>
    /// Retrieves a blog post by its id or its public slug, treating the value as an id when it parses as a GUID and otherwise looking it up as a slug.
    /// </summary>
    /// <param name="idOrSlug">The blog post's GUID id or its slug.</param>
    /// <param name="includeUnpublished">
    /// When false, the default for public callers, a draft post is treated as not found and null is returned; when true, for Admin or Owner, drafts are returned the same as any other post.
    /// </param>
    /// <returns>The blog post with the specified id or slug, or null if not found.</returns>
    Task<BlogPost?> GetBlogPostByIdOrSlugAsync(string idOrSlug, bool includeUnpublished = false);

    /// <summary>
    /// Deletes a blog post as a no-op with no exception when id does not match any post.
    /// </summary>
    /// <param name="id">The id of the blog post to delete.</param>
    Task DeleteBlogPostAsync(Guid id);

    Task UpdateBlogPostAsync(BlogPost blogPostEntity);

    /// <summary>
    /// Retrieves blog posts with pagination and filtering.
    /// </summary>
    /// <param name="filter">The filtering and pagination request.</param>
    /// <param name="includeUnpublished">
    /// When false, the default for public callers, only published posts are returned; when true, for Admin or Owner, drafts are included too.
    /// </param>
    /// <returns>A paginated response containing the blog posts.</returns>
    Task<PaginatedResponse<BlogPost>> GetAllBlogPostsAsync(GetBlogPostsFilteredRequest filter, bool includeUnpublished = false);
}
