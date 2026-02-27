
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.BlogPosts.Request;

/// <summary>
/// Represents a request to create a new blog post, including file upload for the photo.
/// </summary>
public class CreateBlogPostRequest
{
    /// <summary>
    /// The author of the blog post.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public required string Author { get; set; }

    /// <summary>
    /// The title of the blog post.
    /// </summary>
    [Required]
    public required string Title { get; set; }

    /// <summary>
    /// The photo file to upload for the blog post.
    /// </summary>
    public IFormFile? PhotoFile { get; set; }

    /// <summary>
    /// The markdown text content of the blog post.
    /// </summary>
    [Required]
    public required string MarkdownText { get; set; }
}