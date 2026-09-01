using Domain.Enums;

using System;

namespace Domain.Entities.Models;

/// <summary>
/// A member of a team's technical staff (cuerpo técnico — DT, Asistente,
/// DT-Jugador), scoped to one team within one tournament/season, mirroring the
/// Team+Tournament season scoping used by <see cref="PlayerTeamRegistration"/>.
/// Unlike a point deduction, a staff row carries no competitive history worth
/// protecting: it is removed along with either its team or its tournament.
/// </summary>
public class TeamStaff : EntityBase
{
    /// <summary>
    /// The team this staff member belongs to.
    /// </summary>
    public Guid TeamId { get; set; }

    /// <summary>
    /// The team navigation. Optional so the entity can be built from an id
    /// alone; loaded when the caller needs the team's name.
    /// </summary>
    public Team? Team { get; set; }

    /// <summary>
    /// The season (tournament) this staff membership belongs to.
    /// </summary>
    public Guid TournamentId { get; set; }

    /// <summary>
    /// The tournament navigation. Optional so the entity can be built from an
    /// id alone.
    /// </summary>
    public Tournament? Tournament { get; set; }

    /// <summary>
    /// The staff member's full name.
    /// </summary>
    public required string FullName { get; set; }

    /// <summary>
    /// The role this person holds on the team's technical staff.
    /// </summary>
    public required TeamStaffRole Role { get; set; }
}
