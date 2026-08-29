namespace Domain.Entities.Models;

/// <summary>
/// The point-deduction summary attached to a <see cref="Position"/> row when a
/// team carries one or more disciplinary deductions in its division. Aggregates
/// every deduction that targets the team into a single subtracted total and a
/// combined reason, so the standings can show a subtle "-N (motivo)" note.
/// </summary>
public class AppliedPointDeduction
{
    /// <summary>
    /// The total table points subtracted from the team (the sum of every
    /// deduction targeting it). Always positive.
    /// </summary>
    public required int Points { get; set; }

    /// <summary>
    /// The combined disciplinary reason(s). When a team has several
    /// deductions their reasons are joined so the note stays readable.
    /// </summary>
    public required string Reason { get; set; }
}
