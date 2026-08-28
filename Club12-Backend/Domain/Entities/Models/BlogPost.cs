namespace Domain.Entities.Models;

public class BlogPost : EntityBase
{
    public required string Author { get; set; }
    public required string Title { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public blog post links.
    /// Generated once from the title at creation time and never changed
    /// afterward, so shared links keep working even if the title is edited.
    /// </summary>
    public required string Slug { get; set; }

    public int Views { get; set; } = 0;
    public string? PhotoUrl { get; set; }
    public required string MarkdownText { get; set; }

    /// <summary>
    /// Publication state (HU-16). When false the post is a draft: it is
    /// hidden from the public home/list and public detail, and only
    /// authenticated Admin/Owner callers can see it. Defaults to true so
    /// every pre-existing post (and any created without an explicit value)
    /// stays publicly visible — existing content is treated as published.
    /// </summary>
    public bool IsPublished { get; set; } = true;
}