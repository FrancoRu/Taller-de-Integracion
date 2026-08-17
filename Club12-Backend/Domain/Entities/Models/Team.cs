using System;
using System.Collections.Generic;

namespace Domain.Entities.Models;

public class Team : EntityBase
{
    public required string Name { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public team links.
    /// Generated once from the name at creation time and never changed afterward,
    /// so shared links keep working even if the team is renamed.
    /// </summary>
    public required string Slug { get; set; }

    public required string ThreeLetterCode { get; set; }
    public required string LogoUrl { get; set; }
    public required string ShirtColor { get; set; }
    public Tournament? Tournament { get; set; }
    public Guid? TournamentId { get; set; }
    public virtual required ICollection<Player> Players { get; set; } = [];
    public virtual ICollection<StageTeamMatch> StageTeamMatches { get; set; } = [];
}