using System;

namespace Domain.Entities.Models;

public class PlayerSanction : EntityBase
{
    public required int Duration { get; set; }
    public required DateTime IssuedDate { get; set; }
    public required string Description { get; set; }
    public required Player Player { get; set; }
    public Guid PlayerId { get; set; }
    public required Match Match { get; set; }
    public Guid MatchId { get; set; }
}