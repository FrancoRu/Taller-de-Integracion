using Application.DTOs.Abstract.Response;

using System;

namespace Application.DTOs.BlogPosts.Response;

/// <summary>
/// Represents the response data for a blog post.
/// </summary>
public class BlogPostResponse : BaseEntityResponse
{
    public required string Author { get; set; }

    public required string Title { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public blog post links.
    /// </summary>
    public required string Slug { get; set; }

    public int Views { get; set; }

    public string? PhotoUrl { get; set; }

    public required string MarkdownText { get; set; }

    public required DateTime CreatedAt { get; set; }

    /// <summary>
    /// Whether the post is published and visible publicly, or is a draft.
    /// </summary>
    public bool IsPublished { get; set; }
}