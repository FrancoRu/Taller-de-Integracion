using Entities.DTOs.Abstract;
using Entities.DTOs.BlogPost;
using Entities.Models.BlogPostEntity;

namespace Services.Services.BlogPostService;

/// <summary>
/// Represents a service for managing blog posts.
/// </summary>
public interface IBlogPostService
{
    /// <summary>
    /// Creates a new blog post.
    /// </summary>
    /// <param name="blogPostEntity">The blog post entity to create.</param>
    /// <param name="userId">The ID of the user creating the blog post.</param>
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
    /// <param name="blogPostEntity">The blog post to delete.</param>
    Task<bool> DeleteBlogPostAsync(BlogPost blogPostEntity);

    /// <summary>
    /// Updates a blog post asynchronously.
    /// </summary>
    /// <param name="blogPostEntity">The blog post to update.</param>
    /// <param name="userId">The ID of the user updating the blog post.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    Task<bool> UpdateBlogPostAsync(BlogPost blogPostEntity);

    /// <summary>
    /// Retrieves blog posts with pagination and filtering.
    /// </summary>
    /// <param name="filter">The filtering and pagination request.</param>
    /// <returns>A paginated response containing the blog posts.</returns>
    Task<PaginatedResponse<BlogPost>> GetAllBlogPostsAsync(GetBlogPostsFilteredRequest filter);
}
