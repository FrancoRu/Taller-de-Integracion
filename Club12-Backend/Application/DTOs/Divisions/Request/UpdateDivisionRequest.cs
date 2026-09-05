using System;
using System.ComponentModel.DataAnnotations;
namespace Application.DTOs.Divisions.Request;

/// <summary>
/// Represents a request to create a division.
/// </summary>
public class UpdateDivisionRequest
{
    [Required(ErrorMessage = "The Name field is required.")]
    public required string Name { get; set; }

    public required bool IsFinished { get; set; }

    /// <summary>
    /// When provided, moves the division and everything under it to this tournament; null keeps it in place.
    /// </summary>
    public Guid? TournamentId { get; set; }
}
