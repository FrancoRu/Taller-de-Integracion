namespace Domain.Entities.Models;

public class Venue : EntityBase
{
    public required string Name { get; set; }
    public required string Address { get; set; }
    public string? PhotoUrl { get; set; }
}