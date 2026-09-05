namespace Application.DTOs.PointDeductions.Response;

/// <summary>
/// The point-deduction summary carried on a standings position row for a penalised team.
/// </summary>
public class AppliedPointDeductionResponse
{
    /// <summary>
    /// The total table points subtracted from the team.
    /// </summary>
    public int Points { get; set; }

    /// <summary>
    /// The combined disciplinary reasons.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
