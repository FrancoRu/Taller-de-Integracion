using System;

namespace Domain.Entities.Models;

public class Scorer : EntityBase
{
    public required Guid PlayerId { get; set; }

    public Player? Player { get; set; }

    public required int Points { get; set; }

    public required Guid MatchId { get; set; }

    public Match? Match { get; set; }
}
