using System;

namespace Domain.Entities.Models;

/// <summary>
/// Links a Team to a Tournament for exactly one season. This is the source
/// of truth for a team's participation history: unlike
/// <see cref="Team.TournamentId"/> (a denormalized "current season"
/// convenience pointer, always reflecting the team's latest registration), a
/// registration row never changes meaning after it is written. When a Team
/// is reused across seasons by repointing its own
/// <see cref="Team.TournamentId"/> (see TeamService.RegisterTeamsToTournamentAsync),
/// existing registrations keep pointing at the season they were created for,
/// so a team's historical tournament participation is never silently
/// carried over to a new season — e.g. "Colón SF 2026" and "Colón SF 2027"
/// remain two distinct, independently preserved registrations for the same
/// team. A team MAY hold registrations in multiple tournaments at once
/// (enforced only by the unique index on TeamId+TournamentId, which allows
/// multiple rows for the same team as long as the tournament differs).
/// </summary>
public class TeamTournamentRegistration : EntityBase
{
    public required Guid TeamId { get; set; }
    public Team? Team { get; set; }

    /// <summary>
    /// The season this registration belongs to. Captured at registration
    /// time — it is NOT a live reference to Team.TournamentId, so
    /// reassigning the Team to a new tournament later does not
    /// retroactively move this registration.
    /// </summary>
    public required Guid TournamentId { get; set; }
    public Tournament? Tournament { get; set; }
}
