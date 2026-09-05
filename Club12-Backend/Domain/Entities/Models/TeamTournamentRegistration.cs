using System;

namespace Domain.Entities.Models;

/// <summary>
/// Links a Team to a Tournament for exactly one season, the source of truth for a team's participation history, unlike the denormalized Team.TournamentId pointer.
/// </summary>
public class TeamTournamentRegistration : EntityBase
{
    public required Guid TeamId { get; set; }
    public Team? Team { get; set; }

    /// <summary>
    /// The season this registration belongs to, captured at registration time.
    /// </summary>
    public required Guid TournamentId { get; set; }
    public Tournament? Tournament { get; set; }
}
