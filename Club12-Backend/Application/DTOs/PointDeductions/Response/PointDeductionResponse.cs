using System;

namespace Application.DTOs.PointDeductions.Response;

/// <summary>
/// A disciplinary point deduction record, as returned by the create and list endpoints.
/// </summary>
public class PointDeductionResponse
{
    /// <summary>
    /// The unique identifier of the deduction.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The division whose standings the deduction affects.
    /// </summary>
    public Guid DivisionId { get; set; }

    /// <summary>
    /// The penalised team's id.
    /// </summary>
    public Guid TeamId { get; set; }

    /// <summary>
    /// The penalised team's name, when the team was loaded.
    /// </summary>
    public string? TeamName { get; set; }

    /// <summary>
    /// The table points subtracted from the team's total.
    /// </summary>
    public int Points { get; set; }

    /// <summary>
    /// The disciplinary reason for the deduction.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// When the deduction was applied.
    /// </summary>
    public DateTime DateCreated { get; set; }
}
