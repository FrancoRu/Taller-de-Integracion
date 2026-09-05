using Microsoft.AspNetCore.Http;

using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.BlogPosts.Request;

/// <summary>
/// Represents a request to update the photo for a blog post.
/// </summary>
public class UpdateBlogPostPhotoRequest
{
    [Required]
    public required IFormFile PhotoFile { get; set; }
}
