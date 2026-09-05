namespace Application.DTOs.BlogPosts.Request;


/// <summary>
/// Represents a request to update a blog post.
/// </summary>
public class UpdateBlogPostRequest
{
    /// <summary>
    /// Null leaves the current title unchanged.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Null leaves the current Markdown content unchanged.
    /// </summary>
    public string? MarkdownText { get; set; }

    /// <summary>
    /// Null leaves the current author unchanged.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// The updated publication state; null leaves it unchanged, true publishes, false reverts to a draft.
    /// </summary>
    public bool? IsPublished { get; set; }
}
