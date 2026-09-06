using System;

namespace Domain.Entities.Models;

/// <summary>
/// The authoritative record that a team is enrolled in a division, independent of any stage placement.
/// </summary>
public class DivisionTeamRegistration : EntityBase
{
    public required Guid TeamId { get; set; }
    public Team? Team { get; set; }

    public required Guid DivisionId { get; set; }
    public Division? Division { get; set; }
}
