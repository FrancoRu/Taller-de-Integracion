using System.ComponentModel.DataAnnotations;

namespace Entities.DTOs.Team;

/// <summary>
/// Represents a request to update a team's details.
/// </summary>
public class UpdateTeamRequest
{
    /// <summary>
    /// The name of the team.
    /// </summary>
    [Required(ErrorMessage = "The Name field is required.")]
    public required string Name { get; init; }

    /// <summary>
    /// The three-letter code of the team.
    /// </summary>
    [Required(ErrorMessage = "The Three-letter code field is required.")]
    [MaxLength(3)]
    public required string ThreeLetterCode { get; init; }
}
