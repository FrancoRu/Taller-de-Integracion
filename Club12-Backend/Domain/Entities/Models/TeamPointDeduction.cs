using System;

namespace Domain.Entities.Models;

/// <summary>
/// A disciplinary points penalty applied by an admin to a team within a division, subtracted from that team's standings total.
/// </summary>
public class TeamPointDeduction : EntityBase
{
    /// <summary>
    /// The division whose standings the penalty affects.
    /// </summary>
    public Guid DivisionId { get; set; }

    /// <summary>
    /// The division navigation, optional so the entity can be built from an id alone; deleting the division cascades its deductions away.
    /// </summary>
    public Division? Division { get; set; }

    /// <summary>
    /// The penalised team.
    /// </summary>
    public Guid TeamId { get; set; }

    /// <summary>
    /// The team navigation, optional so the entity can be built from an id alone; loaded when the caller needs the team's name.
    /// </summary>
    public Team? Team { get; set; }

    /// <summary>
    /// The positive amount of table points to subtract from the team's total.
    /// </summary>
    public required int Points { get; set; }

    /// <summary>
    /// The disciplinary reason for the deduction, shown next to the penalised team in the standings.
    /// </summary>
    public required string Reason { get; set; }
}
