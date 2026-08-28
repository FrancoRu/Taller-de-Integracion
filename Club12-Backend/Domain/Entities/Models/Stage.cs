using Domain.Enums;

using System;
using System.Collections.Generic;

namespace Domain.Entities.Models;

public class Stage : EntityBase
{
    public required string Name { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public stage links.
    /// Generated once from the name at creation time and never changed
    /// afterward, so shared links keep working even if the stage is renamed.
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
    /// Groups multiple parallel elimination brackets under the same
    /// division (e.g. "Copa de Oro", "Copa de Plata"). Null means the
    /// stage belongs to the division's single/default bracket.
    /// </summary>
    public string? BracketName { get; set; }

    /// <summary>
    /// Number of games in a series between two teams at this round
    /// (1, 3, 5, or 7). 1 means a single match decides the round, matching
    /// all pre-existing elimination stages. Greater values group each
    /// pairing's games into a MatchSeries decided by whichever team wins
    /// the majority.
    /// </summary>
    public int BestOf { get; set; } = 1;
    public virtual ICollection<MatchSeries> MatchSeries { get; set; } = [];

    /// <summary>
    /// How many times each pair of teams plays within this group stage
    /// (1 = single round-robin, 2 = double, ...). Only meaningful for
    /// StageType.Group; ignored for elimination rounds.
    /// </summary>
    public int RoundRobinLegs { get; set; } = 1;
}