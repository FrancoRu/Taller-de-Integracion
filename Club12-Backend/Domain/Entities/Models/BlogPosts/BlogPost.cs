using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Models.BlogPosts;

/// <summary>
/// Represents a blog post in the application.
/// </summary>
[Table("BlogPosts", Schema = "Club12")]
public class BlogPost : EntityBase
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
    /// The number of views the blog post has received.
    /// </summary>
    [DefaultValue(0)]
    public int Views { get; set; } = 0;

    /// <summary>
    /// The URL of the photo associated with the blog post.
    /// </summary>
    [MaxLength(2048)]
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// The markdown text content of the blog post.
    /// </summary>
    [Required]
    public required string MarkdownText { get; set; }
}
