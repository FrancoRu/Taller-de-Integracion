namespace Entities.DTOs.BlogPost;


/// <summary>
/// Represents a request to update a blog post.
/// </summary>
public class UpdateBlogPostRequest
{
    /// <summary>
    /// The updated title of the blog post.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The updated content of the blog post in Markdown format.
    /// </summary>
    public string? MarkdownText { get; set; }

    /// <summary>
    /// The updated author of the blog post.
    /// </summary>
    public string? Author { get; set; }
}
