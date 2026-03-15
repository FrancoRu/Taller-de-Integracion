using System;
using System.Collections.Generic;

namespace Domain.Entities.Models;

public class Team : EntityBase
{
    public required string Name { get; set; }
    public required string ThreeLetterCode { get; set; }
    public required string LogoUrl { get; set; }
    public required string ShirtColor { get; set; }
    public Tournament? Tournament { get; set; }
    public Guid? TournamentId { get; set; }
    public virtual required ICollection<Player> Players { get; set; } = [];
    public virtual ICollection<StageTeamMatch> StageTeamMatches { get; set; } = [];
}