using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.PointDeductions.Request;

/// <summary>
/// Request to apply a disciplinary point deduction to a team within a division taken from the route.
/// </summary>
public class CreatePointDeductionRequest
{
    [Required(ErrorMessage = "The TeamId field is required.")]
    public required Guid TeamId { get; set; }

    /// <summary>
    /// The positive amount of table points to subtract from the team's total.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Points must be at least 1.")]
    public required int Points { get; set; }

    /// <summary>
    /// The disciplinary reason for the deduction.
    /// </summary>
    [Required(ErrorMessage = "The Reason field is required.")]
    [MaxLength(300, ErrorMessage = "Reason cannot exceed 300 characters.")]
    public required string Reason { get; set; }
}
