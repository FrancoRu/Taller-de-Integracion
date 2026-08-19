using Entities.DTOs.Abstract;

namespace Entities.DTOs.BlogPost;

/// <summary>
/// Represents the response data for a blog post.
/// </summary>
public class BlogPostResponse : BaseEntityResponse
{
    /// <summary>
    /// The author of the blog post.
    /// </summary>
    public required string Author { get; set; }

    /// <summary>
    /// The title of the blog post.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// The number of views the blog post has received.
    /// </summary>
    public int Views { get; set; }

    /// <summary>
    /// The URL of the photo associated with the blog post.
    /// </summary>
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// The markdown text content of the blog post.
    /// </summary>
    public required string MarkdownText { get; set; }

    /// <summary>
    /// The date and time the blog post was created.
    /// </summary>
    public required DateTime CreatedAt { get; set; }
}