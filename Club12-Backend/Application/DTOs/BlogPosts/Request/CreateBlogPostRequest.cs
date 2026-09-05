
using Microsoft.AspNetCore.Http;

using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.BlogPosts.Request;

/// <summary>
/// Represents a request to create a new blog post, including file upload for the photo.
/// </summary>
public class CreateBlogPostRequest
{
    [Required]
    [MaxLength(50)]
    public required string Author { get; set; }

    [Required]
    public required string Title { get; set; }

    public IFormFile? PhotoFile { get; set; }

    [Required]
    public required string MarkdownText { get; set; }

    /// <summary>
    /// Whether the post is published or a draft; defaults to true so existing create flows are unaffected.
    /// </summary>
    public bool IsPublished { get; set; } = true;
}