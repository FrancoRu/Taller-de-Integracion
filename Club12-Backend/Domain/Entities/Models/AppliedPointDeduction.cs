namespace Domain.Entities.Models;

/// <summary>
/// The point-deduction summary attached to a Position row when a team carries one or more disciplinary deductions in its division.
/// </summary>
public class AppliedPointDeduction
{
    /// <summary>
    /// The total table points subtracted from the team, always positive.
    /// </summary>
    public required int Points { get; set; }

    /// <summary>
    /// The combined disciplinary reasons, joined when a team has several deductions.
    /// </summary>
    public required string Reason { get; set; }
}
