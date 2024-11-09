using Entities.DTOs.Abstract;
using Entities.DTOs.Player;

namespace Entities.DTOs.Team;

/// <summary>
/// Represents a response for a team, inheriting from the base response.
/// </summary>
public class TeamResponse : BaseEntityResponse
{
    /// <summary>
    /// The name of the team.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The three-letter code of the team.
    /// </summary>
    public required string ThreeLetterCode { get; set; }

    /// <summary>
    /// The unique identifier of the division to which the team belongs.
    /// </summary>
    public required Guid DivisionId { get; set; }

    /// <summary>
    /// The URL of the team's logo.
    /// </summary>
    public required string LogoUrl { get; set; }

    /// <summary>
    /// The list of players in the team.
    /// </summary>
    public required List<PlayerResponse> Players { get; set; } = [];
}
