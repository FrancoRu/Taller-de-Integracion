using System;
using System.ComponentModel.DataAnnotations;
namespace Application.DTOs.Divisions.Request;

/// <summary>
/// Represents a request to create a division.
/// </summary>
public class UpdateDivisionRequest
{
    /// <summary>
    /// The name of the division.
    /// </summary>
    [Required(ErrorMessage = "The Name field is required.")]
    public required string Name { get; set; }

    /// <summary>
    /// Indicates whether the division is finished.
    /// </summary>
    public required bool IsFinished { get; set; }

    /// <summary>
    /// Optional. When provided, moves the division — and everything under
    /// it (stages, matches, team assignments) — to the tournament with this
    /// id, since none of that data carries its own tournament reference.
    /// Leave null to keep the division on its current tournament.
    /// </summary>
    public Guid? TournamentId { get; set; }
}
