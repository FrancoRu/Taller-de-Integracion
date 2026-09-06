using Domain.Enums;

using System;
using System.Collections.Generic;

namespace Domain.Entities.Models;

public class Stage : EntityBase
{
    public required string Name { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public stage links, generated once from the name and never changed afterward.
    /// </summary>
    public required string Slug { get; set; }

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

    /// <summary>
    /// Groups multiple parallel elimination brackets under the same division, null when the stage belongs to the division's default bracket.
    /// </summary>
    public string? BracketName { get; set; }

    /// <summary>
    /// Number of games in a series between two teams at this round, defaulting to 1 for a single match deciding the round.
    /// </summary>
    public int BestOf { get; set; } = 1;
    public virtual ICollection<MatchSeries> MatchSeries { get; set; } = [];

    /// <summary>
    /// How many times each pair of teams plays within this group stage, defaulting to 1 for a single round-robin.
    /// </summary>
    public int RoundRobinLegs { get; set; } = 1;

    /// <summary>
    /// When this bracket's seeding draw was committed, null until a draw runs.
    /// </summary>
    public DateTime? DrawnAt { get; set; }
}