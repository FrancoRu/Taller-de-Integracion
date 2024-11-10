using Microsoft.AspNetCore.Http;

using System.ComponentModel.DataAnnotations;

namespace Entities.DTOs.BlogPost;

/// <summary>
/// Represents a request to update the photo for a blog post.
/// </summary>
public class UpdateBlogPostPhotoRequest
{
    /// <summary>
    /// The file of the blog post photo to be updated.
    /// </summary>
    [Required]
    public required IFormFile PhotoFile { get; set; }
}
