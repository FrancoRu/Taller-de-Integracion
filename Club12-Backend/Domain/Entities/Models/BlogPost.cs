namespace Domain.Entities.Models;

public class BlogPost : EntityBase
{
    public required string Author { get; set; }
    public required string Title { get; set; }
    public int Views { get; set; } = 0;
    public string? PhotoUrl { get; set; }
    public required string MarkdownText { get; set; }
}