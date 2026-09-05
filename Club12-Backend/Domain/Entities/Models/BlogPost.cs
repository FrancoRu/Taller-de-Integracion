namespace Domain.Entities.Models;

public class BlogPost : EntityBase
{
    public required string Author { get; set; }
    public required string Title { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public blog post links, generated once from the title and never changed afterward.
    /// </summary>
    public required string Slug { get; set; }

    public int Views { get; set; } = 0;
    public string? PhotoUrl { get; set; }
    public required string MarkdownText { get; set; }

    /// <summary>
    /// Publication state: when false the post is a draft, hidden from public views and visible only to Admin or Owner callers.
    /// </summary>
    public bool IsPublished { get; set; } = true;
}