using System;

namespace Domain.Entities.Models;

public class PlayerStatistic : EntityBase
{
    public required int Value { get; set; }
    public Match? Match { get; set; }
    public required Guid MatchId { get; set; }
    public Player? Player { get; set; }
    public required Guid PlayerId { get; set; }
}