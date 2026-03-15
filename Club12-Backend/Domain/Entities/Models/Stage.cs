using Domain.Enums;
using System;
using System.Collections.Generic;

namespace Domain.Entities.Models;

public class Stage : EntityBase
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required StageType StageType { get; set; }
    public required bool IsActive { get; set; }
    public bool IsElimination { get; set; }
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
    public required Guid DivisionId { get; set; }
    public required Division Division { get; set; }
    public virtual required ICollection<Match> Matches { get; set; } = [];
    public int Order { get; set; }
    public virtual ICollection<StageTeamMatch> StageTeamMatches { get; set; } = [];
}