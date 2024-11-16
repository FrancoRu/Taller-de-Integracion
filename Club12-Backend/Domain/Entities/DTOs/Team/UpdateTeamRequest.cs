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
    public string? Name { get; init; }

    /// <summary>
    /// The three-letter code of the team.
    /// </summary>
    [MaxLength(3)]
    public string? ThreeLetterCode { get; init; }

    /// <summary>
    /// The color of the team's shirt.
    /// </summary>
    public string? ShirtColor { get; set; }
}
