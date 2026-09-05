using Application.DTOs.Abstract.Response;
using Application.DTOs.BlogPosts.Request;

using Domain.Entities.Models;

using System;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

public interface IBlogPostService
{
    /// <summary>
    /// Creates a blog post, deriving a unique slug from its title (appending a
    /// disambiguator if the generated slug collides with an existing one).
    /// Any <see cref="BlogPost.Slug"/> already set on <paramref name="blogPostEntity"/>
    /// is overwritten.
    /// </summary>
    /// <param name="blogPostEntity">The blog post entity to create.</param>
    /// <returns>The created blog post.</returns>
    Task<BlogPost> CreateBlogPostAsync(BlogPost blogPostEntity);

    Task<BlogPost?> GetBlogPostByIdAsync(Guid blogPostId);

    /// <summary>
    /// Retrieves a blog post by its id or its public slug. The value is
    /// treated as an id when it parses as a GUID, otherwise it is looked up
    /// as a slug.
    /// </summary>
    /// <param name="idOrSlug">The blog post's GUID id or its slug.</param>
    /// <param name="includeUnpublished">
    /// When false (the default, public callers) a draft post (HU-16) is
    /// treated as not found and null is returned; when true (Admin/Owner) drafts
    /// are returned like any other post.
    /// </param>
    /// <returns>The blog post with the specified id or slug, or null if not found.</returns>
    Task<BlogPost?> GetBlogPostByIdOrSlugAsync(string idOrSlug, bool includeUnpublished = false);

    /// <summary>
    /// Deletes a blog post. A no-op (no exception) when <paramref name="id"/>
    /// does not match any post.
    /// </summary>
    /// <param name="id">The id of the blog post to delete.</param>
    Task DeleteBlogPostAsync(Guid id);

    Task UpdateBlogPostAsync(BlogPost blogPostEntity);

    /// <summary>
    /// Retrieves blog posts with pagination and filtering.
    /// </summary>
    /// <param name="filter">The filtering and pagination request.</param>
    /// <param name="includeUnpublished">
    /// When false (the default, public callers) only published posts are
    /// returned (HU-16); when true (Admin/Owner) drafts are included too.
    /// </param>
    /// <returns>A paginated response containing the blog posts.</returns>
    Task<PaginatedResponse<BlogPost>> GetAllBlogPostsAsync(GetBlogPostsFilteredRequest filter, bool includeUnpublished = false);
}
