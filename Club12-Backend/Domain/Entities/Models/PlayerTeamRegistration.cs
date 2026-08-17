using System;

namespace Domain.Entities.Models;

/// <summary>
/// Links a Player to a Team for exactly one season (Tournament). This is the
/// source of truth for roster membership: unlike <see cref="Player.TeamId"/>
/// (a denormalized "current team" convenience pointer, always reflecting the
/// player's latest registration), a registration row never changes meaning
/// after it is written. When a Team is reused across seasons by repointing
/// its own <see cref="Team.TournamentId"/> (see TeamService.RegisterTeamsToTournamentAsync),
/// existing registrations keep pointing at the season they were created for,
/// so a player's historical roster membership is never silently carried over
/// to a new season. A player may have at most one registration per
/// Tournament (enforced by a unique index on PlayerId+TournamentId).
/// </summary>
public class PlayerTeamRegistration : EntityBase
{
    public required Guid PlayerId { get; set; }
    public Player? Player { get; set; }

    public required Guid TeamId { get; set; }
    public Team? Team { get; set; }

    /// <summary>
    /// The season this registration belongs to. Captured at registration
    /// time from the Team's TournamentId at that moment — it is NOT a live
    /// reference to Team.TournamentId, so reassigning the Team to a new
    /// tournament later does not retroactively move this registration.
    /// </summary>
    public required Guid TournamentId { get; set; }
    public Tournament? Tournament { get; set; }
}
