using Entities.DTOs.Abstract;

namespace Entities.DTOs.BlogPost;

/// <summary>
/// Represents a request to get filtered blog posts.
/// </summary>
public class GetBlogPostsFilteredRequest : PaginatedFilterRequest
{
    /// <summary>
    /// The author to filter blog posts by.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// The title to filter blog posts by.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// A keyword to search within the markdown text content of the blog post.
    /// </summary>
    public string? Keyword { get; set; }
}
