using Domain.Enums;

using System;

namespace Domain.Entities.Models;

/// <summary>
/// A member of a team's technical staff, scoped to one team within one tournament, mirroring the season scoping used by PlayerTeamRegistration.
/// </summary>
public class TeamStaff : EntityBase
{
    /// <summary>
    /// The team this staff member belongs to.
    /// </summary>
    public Guid TeamId { get; set; }

    /// <summary>
    /// The team navigation, optional so the entity can be built from an id alone; loaded when the caller needs the team's name.
    /// </summary>
    public Team? Team { get; set; }

    /// <summary>
    /// The season this staff membership belongs to.
    /// </summary>
    public Guid TournamentId { get; set; }

    /// <summary>
    /// The tournament navigation, optional so the entity can be built from an id alone.
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
