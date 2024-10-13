using Club12.DTOs.Abstract;

namespace Club12.DTOs.Team;

/// <summary>
/// Represents a response for a team, inheriting from the base response.
/// </summary>
public record TeamResponse : GenericEntity
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
    public required string DivisionId { get; set; }

    /// <summary>
    /// The URL of the team's logo.
    /// </summary>
    public required string LogoUrl { get; set; }
}
