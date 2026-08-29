namespace Application.DTOs.PointDeductions.Response;

/// <summary>
/// The point-deduction summary carried on a standings position row so the
/// public table can show a subtle "-N (motivo)" note next to a penalised team.
/// </summary>
public class AppliedPointDeductionResponse
{
    /// <summary>The total table points subtracted from the team.</summary>
    public int Points { get; set; }

    /// <summary>The combined disciplinary reason(s).</summary>
    public string Reason { get; set; } = string.Empty;
}
