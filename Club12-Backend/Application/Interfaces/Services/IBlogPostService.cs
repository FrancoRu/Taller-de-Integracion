using Application.DTOs.Abstract.Response;
using Application.DTOs.BlogPosts.Request;

using Domain.Entities.Models;

using System;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Represents a service for managing blog posts.
/// </summary>
public interface IBlogPostService
{
    /// <summary>
    /// Creates a new blog post.
    /// </summary>
    /// <param name="blogPostEntity">The blog post entity to create.</param>
    /// <returns>The created blog post.</returns>
    Task<BlogPost> CreateBlogPostAsync(BlogPost blogPostEntity);

    /// <summary>
    /// Retrieves a blog post by its id.
    /// </summary>
    /// <param name="blogPostId">The id of the blog post to retrieve.</param>
    /// <returns>The blog post with the specified id, or null if not found.</returns>
    Task<BlogPost?> GetBlogPostByIdAsync(Guid blogPostId);

    /// <summary>
    /// Deletes a blog post.
    /// </summary>
    /// <param name="id">The id of the blog post to delete.</param>
    Task DeleteBlogPostAsync(Guid id);

    /// <summary>
    /// Updates a blog post asynchronously.
    /// </summary>
    /// <param name="blogPostEntity">The blog post to update.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    Task UpdateBlogPostAsync(BlogPost blogPostEntity);

    /// <summary>
    /// Retrieves blog posts with pagination and filtering.
    /// </summary>
    /// <param name="filter">The filtering and pagination request.</param>
    /// <returns>A paginated response containing the blog posts.</returns>
    Task<PaginatedResponse<BlogPost>> GetAllBlogPostsAsync(GetBlogPostsFilteredRequest filter);
}
