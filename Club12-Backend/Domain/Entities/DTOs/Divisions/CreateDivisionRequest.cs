using System.ComponentModel.DataAnnotations;

namespace Entities.DTOs.Divisions;

/// <summary>
/// Represents a request to create a division.
/// </summary>
public class CreateDivisionRequest
{
    /// <summary>
    /// The name of the division.
    /// </summary>
    [Required(ErrorMessage = "The Name field is required.")]
    public required string Name { get; set; }

    /// <summary>
    /// The Id of the tournament this division belongs to.
    /// </summary>
    [Required(ErrorMessage = "The TournamentId field is required.")]
    public required Guid TournamentId { get; set; }
}
