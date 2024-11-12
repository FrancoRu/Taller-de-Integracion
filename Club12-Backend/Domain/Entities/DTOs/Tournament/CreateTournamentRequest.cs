using System.ComponentModel.DataAnnotations;

namespace Entities.DTOs.Tournament;

/// <summary>
/// Represents a request to create a new tournament.
/// </summary>
public class CreateTournamentRequest
{
    /// <summary>
    /// The description of the tournament.
    /// </summary>
    [Required(ErrorMessage = "Description is required.")]
    public required string Description { get; set; }

    /// <summary>
    /// The name of the tournament.
    /// </summary>
    [Required(ErrorMessage = "Name is required.")]
    public required string Name { get; set; }
}
